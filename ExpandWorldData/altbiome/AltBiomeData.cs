using System.ComponentModel;
using SpawnData = ExpandWorld.Spawn.Data;
using YamlDotNet.Serialization;

namespace ExpandWorldData;

public class AltBiomeYaml
{
  public string name = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public bool enabled = true;
  [DefaultValue("")]
  public string biome = "";
  [DefaultValue("")]
  public string namePrefix = "";
  [DefaultValue("")]
  public string nameSuffix = "";
  [DefaultValue("")]
  public string nameOverride = "";
  [DefaultValue(1f)]
  public float levelUpChanceMultiplier = 1f;
  [DefaultValue("")]
  public string forceMusic = "";
  [DefaultValue("")]
  public string forceEnvironment = "";
  [DefaultValue(null)]
  public BiomeEnvironment[]? addEnvironments;
  [DefaultValue(null)]
  public string[]? blockEnvironments;
  [DefaultValue(null)]
  public SpawnData[]? spawn;
  [DefaultValue(null)]
  public string[]? blockSpawnNames;
  [DefaultValue(null)]
  public VegetationYaml[]? addVegetation;
  [DefaultValue(null)]
  public string[]? blockVegetationNames;
  [DefaultValue(null)]
  public LocationYaml[]? addLocations;
  [DefaultValue(null)]
  public string[]? blockLocationNames;
  [DefaultValue("None")]
  public string terrainTextureOverride = "None";
  [DefaultValue(0f)]
  public float minDistanceFromCenter = 0f;
  [DefaultValue(0)]
  public int minAmountSpawned = 0;
  [DefaultValue(0)]
  public int maxAmountSpawned = 0;
  [DefaultValue(0f)]
  public float chance = 0f;
  [DefaultValue("None")]
  public string requireNeighbor = "None";
  [DefaultValue("None")]
  public string notNeighbor = "None";
  [DefaultValue(null)]
  public string[]? incompatibleAltBiomes;
  [DefaultValue(0)]
  public int minEdgeSize = 0;
  [DefaultValue(0)]
  public int maxEdgeSize = 0;
  [DefaultValue(0f)]
  public float minAvgHeight = 0f;
  [DefaultValue(0f)]
  public float maxAvgHeight = 0f;
  [DefaultValue(0f)]
  public float belowWorldX = 0f;
  [DefaultValue(0f)]
  public float aboveWorldX = 0f;
  [DefaultValue(0f)]
  public float belowWorldY = 0f;
  [DefaultValue(0f)]
  public float aboveWorldY = 0f;
}
