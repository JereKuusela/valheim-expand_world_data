using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using Service;

namespace ExpandWorld.Spawn;

public class SpawnManager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_spawns.yaml");
  public const string Pattern = "expand_spawns*.yaml";
  public static List<SpawnSystem.SpawnData>? Override;
  public static bool HasData => Override != null;

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

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => SpawnManager.FilePath;
    protected override string Pattern => SpawnManager.Pattern;
    protected override bool DataEnabled => Configuration.DataSpawns;
    protected override string ConfigValue { get => Configuration.valueSpawnData.Value; set => Configuration.valueSpawnData.Value = value; }
    protected override bool Set(Dictionary<string, string> files) => SpawnManager.Set(files);
    protected override void WriteDefaultConfig() => ConfigValue = Save();
  }
  private static readonly Sync Instance = new();

  private static bool Set(Dictionary<string, string> files)
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
    if (files.Count == 0) return true;
    if (data.Count == 0) return false;
    Log.Info($"Reloading spawn data ({data.Count} entries).");
    Override = data;
    SpawnSystem.m_instances.ForEach(ApplySpawnData);
    return true;
  }

  internal static void InitializeData()
  {
    Override = null;
    if (Helper.IsServer()) ReadConfigs();
  }

  private static bool defaultsCaptured;

  internal static void InitializeSpawnSystem(SpawnSystem __instance)
  {
    // Lists are still vanilla until ApplySpawnData runs for the first time.
    if (!defaultsCaptured)
    {
      defaultsCaptured = true;
      if (Helper.IsServer() && Configuration.DataMigration && Configuration.DataSpawns)
        MigratePersistentEvents(__instance.m_spawnLists.SelectMany(list => list.m_spawners));
    }
    if (Override == null)
    {
      if (Helper.IsClient() && Configuration.valueSpawnData.Value != "") FromSetting(Configuration.valueSpawnData.Value);
      if (Helper.IsServer()) CreateConfigs();
    }
    ApplySpawnData(__instance);
  }

  ///<summary>Backfills requiredPersistentEvent for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
  private static void MigratePersistentEvents(IEnumerable<SpawnSystem.SpawnData> vanilla)
  {
    if (!File.Exists(FilePath)) return;
    if (!Yaml.TryDeserialize<Data>(File.ReadAllText(FilePath), FilePath, out var existing)) return;
    // Any entry with the field set means the file is up to date.
    if (existing.Any(entry => !string.IsNullOrEmpty(entry.requiredPersistentEvent))) return;
    // Only prefabs whose vanilla entries all agree on one event can be keyed safely.
    var migrations = vanilla
      .Where(spawn => spawn.m_prefab)
      .GroupBy(spawn => spawn.m_prefab.name)
      .Where(group => group.Select(spawn => spawn.m_requiredPersistentEvent).Distinct().Count() == 1 && !string.IsNullOrEmpty(group.First().m_requiredPersistentEvent))
      .ToDictionary(group => group.Key, group => group.First().m_requiredPersistentEvent);
    if (migrations.Count == 0) return;
    if (!Yaml.InsertMissingField(Pattern, "prefab", "requiredPersistentEvent", migrations)) return;
    Log.Warning($"Added requiredPersistentEvent to {migrations.Count} prefabs in {Pattern} files.");
    ReadConfigs();
  }

  public static void ApplySpawnData(SpawnSystem system)
  {
    if (Override == null) return;
    while (system.m_spawnLists.Count > 1) system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
    system.m_spawnLists[0].m_spawners = Override;
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataSpawns, ReadConfigs);
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