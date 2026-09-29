using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorld.Spawn;
using Service;

namespace ExpandWorldData;

public static class AltBiomeManager
{
  public static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, "expand_altbiomes.yaml");
  public const string Pattern = "expand_altbiomes*.yaml";
  private static List<AltBiome> Original = [];
  private static List<AltBiome> Active = [];

  public static void Initialize()
  {
    Original = [.. AltBiomeList.m_altBiomes];
    Active = [.. Original];
  }

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => AltBiomeManager.FilePath;
    protected override string Pattern => AltBiomeManager.Pattern;
    protected override bool DataEnabled => Configuration.DataAltBiomes;
    protected override string ConfigValue { get => Configuration.valueAltBiomeData.Value; set => Configuration.valueAltBiomeData.Value = value; }
    protected override bool Set(Dictionary<string, string> files) => AltBiomeManager.Set(files);
    protected override void WriteDefaultConfig() => File.WriteAllText(FilePath, Yaml.Serializer().Serialize(Original.Select(ToData).ToList()));
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private static bool Set(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      Apply(Original, true);
      return true;
    }
    try
    {
      List<AltBiome> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<AltBiomeYaml>(file.Value, file.Key, out var parsed)) return false;
        data.AddRange(parsed.Select(item => FromData(item, file.Key)));
      }
      if (data.Count == 0)
      {
        Log.Warning("Failed to load any alt biome data. No changes done.");
        return false;
      }
      Apply(data, true);
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return false;
    }
  }


  private static void Apply(List<AltBiome> data, bool regenerate)
  {
    Active = data;
    AltBiomeList.m_altBiomes.Clear();
    AltBiomeList.m_altBiomes.AddRange(Active);
    Log.Info($"Reloading alt biome data ({data.Count} entries).");
    if (regenerate)
      Refresh.World();
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataAltBiomes, ReadConfigs);

  public static AltBiome FromData(AltBiomeYaml data, string fileName = "AltBiomes")
  {
    var alt = new AltBiome
    {
      m_name = data.name,
      m_enabled = data.enabled,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_namePrefix = data.namePrefix,
      m_nameSuffix = data.nameSuffix,
      m_nameOverride = data.nameOverride,
      m_levelUpChanceMultiplier = data.levelUpChanceMultiplier,
      m_forceMusic = data.forceMusic,
      m_forceEnvironment = data.forceEnvironment,
      m_blockEnvironments = data.blockEnvironments?.ToList() ?? [],
      m_blockSpawnNames = data.blockSpawnNames?.ToList() ?? [],
      m_blockVegetationNames = data.blockVegetationNames?.ToList() ?? [],
      m_blockLocationNames = data.blockLocationNames?.ToList() ?? [],
      m_minDistanceFromCenter = data.minDistanceFromCenter,
      m_minAmountSpawned = data.minAmountSpawned,
      m_maxAmountSpawned = data.maxAmountSpawned,
      m_chance = data.chance,
      m_requireNeighbor = DataManager.ToBiomes(data.requireNeighbor, fileName),
      m_notNeighbor = DataManager.ToBiomes(data.notNeighbor, fileName),
      m_incompatibleAltBiomes = data.incompatibleAltBiomes?.ToList() ?? [],
      m_minEdgeSize = data.minEdgeSize,
      m_maxEdgeSize = data.maxEdgeSize,
      m_minAvgHeight = data.minAvgHeight,
      m_maxAvgHeight = data.maxAvgHeight,
      m_belowWorldX = data.belowWorldX,
      m_aboveWorldX = data.aboveWorldX,
      m_belowWorldY = data.belowWorldY,
      m_aboveWorldY = data.aboveWorldY,
      m_terrainTextureOverride = DataManager.ToBiomes(data.terrainTextureOverride, fileName)
    };
    if (data.addEnvironments != null)
      alt.m_addEnvironments = [.. data.addEnvironments.Select(item => BiomeManager.FromData(item, []))];
    if (data.spawn != null)
      alt.m_spawn = [.. data.spawn.Select(item => Loader.FromData(item, fileName))];
    if (data.addVegetation != null)
      alt.m_addVegetation = [.. data.addVegetation.Select(item => VegetationManager.FromData(item, fileName))];
    if (data.addLocations != null)
      alt.m_addLocations = [.. data.addLocations.Select(item => LocationManager.FromData(item, fileName))];
    return alt;
  }

  public static AltBiomeYaml ToData(AltBiome alt) => new()
  {
    name = alt.m_name,
    enabled = alt.m_enabled,
    biome = DataManager.FromBiomes(alt.m_biome),
    namePrefix = alt.m_namePrefix,
    nameSuffix = alt.m_nameSuffix,
    nameOverride = alt.m_nameOverride,
    levelUpChanceMultiplier = alt.m_levelUpChanceMultiplier,
    forceMusic = alt.m_forceMusic,
    forceEnvironment = alt.m_forceEnvironment,
    blockEnvironments = alt.m_blockEnvironments.Count > 0 ? [.. alt.m_blockEnvironments] : null,
    blockSpawnNames = alt.m_blockSpawnNames.Count > 0 ? [.. alt.m_blockSpawnNames] : null,
    blockVegetationNames = alt.m_blockVegetationNames.Count > 0 ? [.. alt.m_blockVegetationNames] : null,
    blockLocationNames = alt.m_blockLocationNames.Count > 0 ? [.. alt.m_blockLocationNames] : null,
    minDistanceFromCenter = alt.m_minDistanceFromCenter,
    minAmountSpawned = alt.m_minAmountSpawned,
    maxAmountSpawned = alt.m_maxAmountSpawned,
    chance = alt.m_chance,
    requireNeighbor = DataManager.FromBiomes(alt.m_requireNeighbor),
    notNeighbor = DataManager.FromBiomes(alt.m_notNeighbor),
    incompatibleAltBiomes = alt.m_incompatibleAltBiomes.Count > 0 ? [.. alt.m_incompatibleAltBiomes] : null,
    minEdgeSize = alt.m_minEdgeSize,
    maxEdgeSize = alt.m_maxEdgeSize,
    minAvgHeight = alt.m_minAvgHeight,
    maxAvgHeight = alt.m_maxAvgHeight,
    belowWorldX = alt.m_belowWorldX,
    aboveWorldX = alt.m_aboveWorldX,
    belowWorldY = alt.m_belowWorldY,
    aboveWorldY = alt.m_aboveWorldY,
    terrainTextureOverride = DataManager.FromBiomes(alt.m_terrainTextureOverride),
    addEnvironments = alt.m_addEnvironments.Count > 0 ? [.. alt.m_addEnvironments.Select(BiomeManager.ToData)] : null,
    spawn = alt.m_spawn.Count > 0 ? [.. alt.m_spawn.Select(Loader.ToData)] : null,
    addVegetation = alt.m_addVegetation.Count > 0 ? [.. alt.m_addVegetation.Select(VegetationManager.ToData)] : null,
    addLocations = alt.m_addLocations.Count > 0 ? [.. alt.m_addLocations.Select(LocationManager.ToData)] : null
  };
}
