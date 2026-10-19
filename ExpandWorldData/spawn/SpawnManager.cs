using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Data;
using ExpandWorldData.Migration;
using Service;

namespace ExpandWorldData.Spawn;

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
    using var metadata = new MetadataLoad();
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
      if (Helper.IsServer())
      {
        Migrations.RunSpawns(Defaults, ReadConfigs);
        CreateConfigs();
      }
    }
    ApplySpawnData(__instance);
  }

  public static void ApplySpawnData(SpawnSystem system)
  {
    if (Override != null)
    {
      while (system.m_spawnLists.Count > 1) system.m_spawnLists.RemoveAt(system.m_spawnLists.Count - 1);
      // Reference is kept to improve compatibility with other mods.
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