using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using Service;

namespace ExpandWorld.Spawn;

public class Manager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_spawns.yaml");
  public const string Pattern = "expand_spawns*.yaml";
  public static List<SpawnSystem.SpawnData>? Override;

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
    if (!Configuration.DataSpawns || !Helper.IsClient()) return;
    Set(yaml);
  }

  public static void Set(string yaml)
  {
    if (Override != null)
    {
      foreach (var spawn in Override)
      {
        Loader.Data.Remove(spawn);
        Loader.Objects.Remove(spawn);
      }
    }
    Override = null;
    try
    {
      if (!Configuration.DataSpawns || yaml == "")
        return;
      var data = Yaml.Deserialize<Data>(yaml, "Spawns").Select(entry => Loader.FromData(entry, "Spawns")).Where(IsValid).ToList();
      if (data.Count == 0)
      {
        Log.Warning("Failed to load any spawn data. No changes done.");
        return;
      }
      Log.Info($"Reloading spawn data ({data.Count} entries).");
      Override = data;
      SpawnSystem.m_instances.ForEach(ApplySpawnData);
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    finally { ExpandWorldData.Patcher.Update(EWD.Harmony); }
  }

  public static void Toggle()
  {
    if (Configuration.DataSpawns)
    {
      ExpandWorldData.Patcher.Update(EWD.Harmony);
      if (Helper.IsServer()) ReadConfig();
      else FromSetting(Configuration.valueSpawnData.Value);
    }
    else
    {
      Log.Warning("Disabling spawn data requires a restart to restore the original spawn lists.");
      ExpandWorldData.Patcher.Update(EWD.Harmony);
    }
  }

  internal static void InitializeData()
  {
    Override = null;
    if (Helper.IsServer()) ReadConfig();
  }

  internal static void InitializeSpawnSystem(SpawnSystem __instance)
  {
    if (Override == null)
    {
      if (Helper.IsClient() && Configuration.valueSpawnData.Value != "") Set(Configuration.valueSpawnData.Value);
      if (Helper.IsServer()) CreateConfig();
    }
    ApplySpawnData(__instance);
  }

  public static void ApplySpawnData(SpawnSystem system)
  {
    if (Override == null) return;
    while (system.m_spawnLists.Count > 1) system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
    system.m_spawnLists[0].m_spawners = Override;
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfig);
}

public class GlobalKeys
{
  internal static void Mutate(ZoneSystem __instance, ref string name)
  {
    var key = ZoneSystem.GetKeyValue(name.ToLower(), out var value, out _);
    if (value.StartsWith("--", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var decrease))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue - decrease}" : $"{key} -{decrease}";
    else if (value.StartsWith("++", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var increase))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue + increase}" : $"{key} {increase}";
  }
  internal static bool CheckRequirement(ZoneSystem __instance, string name, ref bool __result)
  {
    var split = name.Trim().Split(' ');
    if (split.Length < 2 || !int.TryParse(split[1], out var requiredValue)) return true;
    __result = __instance.m_globalKeysValues.TryGetValue(split[0].ToLower(), out var rawValue) && int.TryParse(rawValue, out var value) && value >= requiredValue;
    return false;
  }
  internal static void Consume(SpawnSystem.SpawnData critter)
  {
    if (string.IsNullOrEmpty(critter.m_requiredGlobalKey)) return;
    var split = critter.m_requiredGlobalKey.Trim().Split(' ');
    if (split.Length > 1 && int.TryParse(split[1], out var amount)) ZoneSystem.instance.SetGlobalKey($"{split[0]} --{amount}");
  }
}