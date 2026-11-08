using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Spawn;
using Service;
using Common;
using ExpandWorldData.Data;

namespace ExpandWorldData.AltBiomes;

public static partial class AltBiomeManager
{
  public static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, "expand_altbiomes.yaml");
  public const string Pattern = "expand_altbiomes*.yaml";
  private static List<AltBiome> Original = [];
  private static List<AltBiome> Active = [];

  public static IEnumerable<SpawnSystem.SpawnData> ActiveSpawns => Active.SelectMany(alt => alt.m_spawn);
  public static void Initialize()
  {
    Original = [.. AltBiomeList.m_altBiomes];
    Active = Original;
  }

  // Static list must be cleared. Otherwise each load adds duplicate entries.
  public static void CleanUp()
  {
    foreach (var spawn in Active.SelectMany(alt => alt.m_spawn))
    {
      Loader.Data.Remove(spawn);
      Loader.Objects.Remove(spawn);
      Loader.Requirements.Remove(spawn);
    }
    Active = [];
    AltBiomeList.m_altBiomes.Clear();
  }

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => AltBiomeManager.FilePath;
    protected override string Pattern => AltBiomeManager.Pattern;
    protected override bool DataEnabled => Configuration.DataAltBiomes;
    protected override string ConfigValue { get => Configuration.valueAltBiomeData.Value; set => Configuration.valueAltBiomeData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => AltBiomeManager.Set(files);
    protected override void WriteDefaultConfig() => File.WriteAllText(FilePath, Yaml.Serializer().Serialize(Original.Select(ToData).ToList()));
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private static SetResult Set(Dictionary<string, string> files)
  {
    using var metadata = new MetadataLoad();
    var previous = Active;
    var result = LoadAndApply(files);
    if (result == SetResult.Applied)
      metadata.Commit(previous.SelectMany(alt => alt.m_spawn), Active.SelectMany(alt => alt.m_spawn));
    return result;
  }

  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      Apply(Original, true);
      return SetResult.Applied;
    }
    if (!Yaml.TryDeserialize<AltBiomeYaml, AltBiome>(files, FromData, out var data)) return SetResult.Failed;
    if (data.Count == 0)
    {
      Log.Warning("Failed to load any alt biome data. No changes done.");
      return SetResult.Failed;
    }
    Apply(data, true);
    return SetResult.Applied;
  }


  private static void Apply(List<AltBiome> data, bool regenerate)
  {
    Active = data;
    AltBiomeList.m_altBiomes.Clear();
    AltBiomeList.m_altBiomes.AddRange(Active);
    Log.Info($"Reloading alt biome data ({data.Count} entries).");
    // Alt biomes can force or block environments, so the current weather must be rerolled.
    if (EnvMan.instance != null) EnvMan.instance.m_environmentPeriod = -1;
    if (regenerate)
      Refresh.Request(Regen.World);
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataAltBiomes, ReadConfigs);
}
