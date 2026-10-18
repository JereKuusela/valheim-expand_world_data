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
  // Vanilla lists, captured before they are first replaced. The list assets are shared and persist across worlds.
  private static List<(SpawnSystemList List, List<SpawnSystem.SpawnData> Spawners)>? Defaults;
  private static bool Modified;

  public static bool IsValid(SpawnSystem.SpawnData spawn) => spawn.m_prefab;

  public static string Save()
  {
    if (Defaults == null) return "";
    var spawns = Defaults.SelectMany(entry => entry.Spawners);
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
    protected override SetResult Set(Dictionary<string, string> files) => SpawnManager.Set(files);
    protected override void WriteDefaultConfig() => Save();
  }
  private static readonly Sync Instance = new();

  private static SetResult Set(Dictionary<string, string> files)
  {
    using var metadata = new Loader.MetadataLoad();
    var previous = Override;
    var result = LoadAndApply(files);
    if (result == SetResult.Applied)
      metadata.Commit(previous ?? [], Override ?? []);
    return result;
  }

  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    if (!Yaml.TryDeserialize<Data, SpawnSystem.SpawnData>(files, Loader.FromData, out var parsed)) return SetResult.Failed;
    var data = parsed.Where(IsValid).ToList();
    if (files.Count > 0 && data.Count == 0) return SetResult.Failed;
    if (files.Count == 0)
    {
      Log.Info("Reloading default spawn data.");
      Override = null;
    }
    else
    {
      Log.Info($"Reloading spawn data ({data.Count} entries).");
      Override = data;
    }
    SpawnSystem.m_instances.ForEach(ApplySpawnData);
    // Instances created later get the restored shared lists.
    if (Override == null) Modified = false;
    return SetResult.Applied;
  }

  internal static void InitializeData()
  {
    if (Helper.IsServer()) ReadConfigs();
  }

  internal static void InitializeSpawnSystem(SpawnSystem __instance)
  {
    // Lists are still vanilla until ApplySpawnData runs for the first time.
    if (Defaults == null)
    {
      Defaults = [.. __instance.m_spawnLists.Select(list => (list, list.m_spawners))];
    }
    if (Helper.IsServer())
    {
      Migrations.RunSpawns(() => MigratePersistentEvents(Defaults.SelectMany(entry => entry.Spawners)));
      CreateConfigs();
    }
    ApplySpawnData(__instance);
  }

  ///<summary>Backfills requiredPersistentEvent for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
  private static void MigratePersistentEvents(IEnumerable<SpawnSystem.SpawnData> vanilla)
  {
    var files = Migrations.ReadFiles<Data>(Pattern);
    var eligible = files.Keys.Where(file => !Yaml.HasField([file], "requiredPersistentEvent")).ToList();
    if (!BackfillPersistentEvents(vanilla, eligible)) return;
    Yaml.IgnoreWrites(Pattern);
    ReadConfigs();
  }

  private static bool BackfillPersistentEvents(IEnumerable<SpawnSystem.SpawnData> vanilla, List<string> files)
  {
    // Only prefabs whose vanilla entries all agree on one event can be keyed safely.
    var migrations = vanilla
      .Where(spawn => spawn.m_prefab)
      .GroupBy(spawn => spawn.m_prefab.name)
      .Where(group => group.Select(spawn => spawn.m_requiredPersistentEvent).Distinct().Count() == 1 && !string.IsNullOrEmpty(group.First().m_requiredPersistentEvent))
      .ToDictionary(group => group.Key, group => group.First().m_requiredPersistentEvent);
    if (migrations.Count == 0) return false;
    if (!Yaml.InsertMissingField(files, "prefab", "requiredPersistentEvent", migrations)) return false;
    Log.Warning($"Added requiredPersistentEvent to {migrations.Count} prefabs in {Pattern} files.");
    return true;
  }

  public static void ApplySpawnData(SpawnSystem system)
  {
    if (Override != null)
    {
      while (system.m_spawnLists.Count > 1) system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
      system.m_spawnLists[0].m_spawners = Override;
      Modified = true;
    }
    else if (Modified && Defaults != null)
    {
      system.m_spawnLists = [.. Defaults.Select(entry => entry.List)];
      foreach (var entry in Defaults) entry.List.m_spawners = entry.Spawners;
    }
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataSpawns, ReadConfigs);
}