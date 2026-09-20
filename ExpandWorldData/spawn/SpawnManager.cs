using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;

namespace ExpandWorld.Spawn;

public class Manager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_spawns.yaml");
  public const string Pattern = "expand_spawns*.yaml";

  public static bool IsValid(SpawnSystem.SpawnData spawn) => spawn.m_prefab;

  public static string Save()
  {
    var spawnSystem = SpawnSystem.m_instances.FirstOrDefault();
    if (spawnSystem == null) return "";
    var spawns = spawnSystem.m_spawnLists.SelectMany(list => list.m_spawners);
    var yaml = Yaml.Serializer().Serialize(spawns.Select(Loader.ToData).ToList());
    File.WriteAllText(FilePath, yaml);
    return yaml;
  }

  public static void CreateConfig()
  {
    if (!Configuration.DataSpawns || Helper.IsClient() || File.Exists(FilePath)) return;
    Configuration.valueSpawnData.Value = Save();
  }

  public static void ReadConfig()
  {
    if (!Configuration.DataSpawns || Helper.IsClient()) return;
    var yaml = DataManager.Read<Data, SpawnSystem.SpawnData>(Pattern, Loader.FromData);
    Configuration.valueSpawnData.Value = yaml;
    Set(yaml);
  }

  public static void FromSetting(string yaml)
  {
    if (!Configuration.DataSpawns || HandleSpawnData.Override == null || !Helper.IsClient()) return;
    Set(yaml);
  }

  public static void Set(string yaml)
  {
    HandleSpawnData.Override = null;
    Loader.Data.Clear();
    Loader.Objects.Clear();
    if (!Configuration.DataSpawns || yaml == "")
    {
      HandleSpawnData.RestoreAll();
      return;
    }
    try
    {
      var data = Yaml.Deserialize<Data>(yaml, "Spawns").Select(entry => Loader.FromData(entry, "Spawns")).Where(IsValid).ToList();
      if (data.Count == 0)
      {
        Log.Warning("Failed to load any spawn data.");
        HandleSpawnData.RestoreAll();
        return;
      }
      Log.Info($"Reloading spawn data ({data.Count} entries).");
      HandleSpawnData.Override = data;
      SpawnSystem.m_instances.ForEach(HandleSpawnData.Set);
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }

  public static void Toggle()
  {
    if (Configuration.DataSpawns)
    {
      if (Helper.IsServer()) ReadConfig();
      else FromSetting(Configuration.valueSpawnData.Value);
    }
    else
      Set("");
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfig);
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeSpawnContent
{
  static void Postfix()
  {
    if (!Configuration.DataSpawns) return;
    HandleSpawnData.Override = null;
    if (Helper.IsServer()) Manager.ReadConfig();
  }
}

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
public class HandleSpawnData
{
  public static List<SpawnSystem.SpawnData>? Override;
  private static readonly Dictionary<SpawnSystem, List<SpawnSystemList>> Originals = [];

  static void Postfix(SpawnSystem __instance)
  {
    Originals[__instance] = [.. __instance.m_spawnLists];
    if (!Configuration.DataSpawns) return;
    if (Override == null)
    {
      if (Helper.IsClient() && Configuration.valueSpawnData.Value != "") Manager.Set(Configuration.valueSpawnData.Value);
      if (Helper.IsServer()) Manager.CreateConfig();
    }
    Set(__instance);
  }

  public static void Set(SpawnSystem system)
  {
    if (Override == null) return;
    while (system.m_spawnLists.Count > 1) system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
    system.m_spawnLists[0].m_spawners = Override;
  }

  public static void RestoreAll()
  {
    foreach (var pair in Originals)
      pair.Key.m_spawnLists = [.. pair.Value];
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey))]
public class SpawnGlobalKeyMutation
{
  static void Prefix(ZoneSystem __instance, ref string name)
  {
    if (!Configuration.DataSpawns) return;
    var key = ZoneSystem.GetKeyValue(name.ToLower(), out var value, out _);
    if (value.StartsWith("--", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var decrease))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue - decrease}" : $"{key} -{decrease}";
    else if (value.StartsWith("++", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var increase))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue + increase}" : $"{key} {increase}";
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), typeof(string))]
public class SpawnGlobalKeyCheck
{
  static bool Prefix(ZoneSystem __instance, string name, ref bool __result)
  {
    if (!Configuration.DataSpawns) return true;
    var split = name.Trim().Split(' ');
    if (split.Length < 2 || !int.TryParse(split[1], out var requiredValue)) return true;
    __result = __instance.m_globalKeysValues.TryGetValue(split[0].ToLower(), out var rawValue) && int.TryParse(rawValue, out var value) && value >= requiredValue;
    return false;
  }
}

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
public class ConsumeSpawnGlobalKey
{
  static void Postfix(SpawnSystem.SpawnData critter)
  {
    if (!Configuration.DataSpawns || string.IsNullOrEmpty(critter.m_requiredGlobalKey)) return;
    var split = critter.m_requiredGlobalKey.Trim().Split(' ');
    if (split.Length > 1 && int.TryParse(split[1], out var amount)) ZoneSystem.instance.SetGlobalKey($"{split[0]} --{amount}");
  }
}