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

  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (!Configuration.DataSpawns)
    {
      if (Set([]))
        Configuration.valueSpawnData.Value = "";
      return;
    }
    var files = DataManager.Read(Pattern);
    if (files == null || !Set(files)) return;
    Configuration.valueSpawnData.Value = string.Join("\n", files.Values);
  }

  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }

  public static bool Set(Dictionary<string, string> files)
  {
    List<SpawnSystem.SpawnData> data = [];
    try
    {
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<Data>(file.Value, file.Key, out var parsed)) return false;
        data.AddRange(parsed.Select(entry => Loader.FromData(entry, file.Key)).Where(IsValid));
      }
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return false;
    }
    if (Override != null)
    {
      foreach (var spawn in Override)
      {
        Loader.Data.Remove(spawn);
        Loader.Objects.Remove(spawn);
      }
    }
    Override = null;
    if (files.Count == 0)
    {
      Patcher.SetEnabled(false);
      EWD.Instance.InvokeRegenerate();
      return true;
    }
    if (data.Count == 0) return false;
    Log.Info($"Reloading spawn data ({data.Count} entries).");
    Override = data;
    Patcher.SetEnabled(true);
    SpawnSystem.m_instances.ForEach(ApplySpawnData);
    EWD.Instance.InvokeRegenerate();
    return true;
  }

  internal static void InitializeData()
  {
    Override = null;
    if (Helper.IsServer()) ReadConfigs();
  }

  internal static void InitializeSpawnSystem(SpawnSystem __instance)
  {
    if (Override == null)
    {
      if (Helper.IsClient() && Configuration.valueSpawnData.Value != "") FromSetting(Configuration.valueSpawnData.Value);
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

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfigs);
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