using System.ComponentModel;
using SpawnData = ExpandWorld.Spawn.Data;
using YamlDotNet.Serialization;

namespace ExpandWorldData;

public class AltBiomeYaml
{
  public string name = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public int amount = 80;
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
  [DefaultValue(1000f)]
  public float minDistanceFromCenter = 1000f;
  [DefaultValue(1)]
  public int minAmountSpawned = 1;
  [DefaultValue(10)]
  public int maxAmountSpawned = 10;
  [DefaultValue(0.1f)]
  public float chance = 0.1f;
  [DefaultValue("None")]
  public string requireNeighbor = "None";
  [DefaultValue("None")]
  public string notNeighbor = "None";
  [DefaultValue(null)]
  public string[]? incompatibleAltBiomes;
  [DefaultValue(50)]
  public int minEdgeSize = 50;
  [DefaultValue(1500)]
  public int maxEdgeSize = 1500;
  [DefaultValue(30f)]
  public float minAvgHeight = 30f;
  [DefaultValue(10000f)]
  public float maxAvgHeight = 10000f;
}
