using System.ComponentModel;

namespace ExpandWorldData;

public class LocationData
{
  public string prefab = "";
  public bool enabled = true;
  [DefaultValue("")]
  public string dungeon = "";
  [DefaultValue("")]
  public string biome = "";
  [DefaultValue("")]
  public string biomeArea = "";
  public int quantity = 0;
  [DefaultValue(0f)]
  public float minDistance = 0f;
  [DefaultValue(0f)]
  public float maxDistance = 0f;
  [DefaultValue(-1000f)]
  public float minAltitude = -1000f;
  [DefaultValue(10000f)]
  public float maxAltitude = 10000f;
  [DefaultValue(false)]
  public bool prioritized = false;
  [DefaultValue(false)]
  public bool centerFirst = false;
  [DefaultValue(false)]
  public bool unique = false;
  [DefaultValue(false)]
  public bool randomSeed = false;
  [DefaultValue("")]
  public string group = "";
  [DefaultValue("")]
  public string groupMax = "";
  [DefaultValue(0f)]
  public float minDistanceFromSimilar = 0f;
  [DefaultValue("")]
  public string discoverLabel = "";
  [DefaultValue("")]
  public string iconAlways = "";
  [DefaultValue("")]
  public string iconPlaced = "";
  [DefaultValue(false)]
  public bool randomRotation = false;
  [DefaultValue(false)]
  public bool slopeRotation = false;
  [DefaultValue(false)]
  public bool snapToWater = false;
  [DefaultValue(0f)]
  public float minTerrainDelta = 0f;
  [DefaultValue(10f)]
  public float maxTerrainDelta = 10f;
  [DefaultValue(false)]
  public bool inForest = false;
  [DefaultValue(0f)]
  public float forestTresholdMin = 0f;
  [DefaultValue(1f)]
  public float forestTresholdMax = 1f;
  [DefaultValue(null)]
  public float? offset = null;
  [DefaultValue(0f)]
  public float groundOffset = 0f;
  [DefaultValue("")]
  public string data = "";
  [DefaultValue(null)]
  public string[]? objectData = null;
  [DefaultValue(null)]
  public string[]? objectSwap = null;
  [DefaultValue(null)]
  public string[]? dungeonObjectData = null;
  [DefaultValue(null)]
  public string[]? dungeonObjectSwap = null;
  [DefaultValue(null)]
  public string[]? locationObjectData = null;
  [DefaultValue(null)]
  public string[]? locationObjectSwap = null;
  [DefaultValue(null)]
  public string[]? objects = null;
  [DefaultValue(null)]
  public string[]? commands = null;
  [DefaultValue(0f)]
  public float exteriorRadius = 0f;
  [DefaultValue(false)]
  public bool clearArea = false;
  [DefaultValue("")]
  public string randomDamage = "";
  [DefaultValue("")]
  public string noBuild = "";
  [DefaultValue("")]
  public string noBuildDungeon = "";
  [DefaultValue("")]
  public string levelArea = "";
  [DefaultValue(0f)]
  public float levelRadius = 0f;
  [DefaultValue(0f)]
  public float levelBorder = 0f;
  [DefaultValue("")]
  public string paint = "";
  [DefaultValue(null)]
  public float? paintRadius = null;
  [DefaultValue(null)]
  public float? paintBorder = null;
  [DefaultValue("1")]
  public string scaleMin = "1";
  [DefaultValue("1")]
  public string scaleMax = "1";
  [DefaultValue(true)]
  public bool scaleUniform = true;
  [DefaultValue(0f)]
  public float maxDistanceFromSimilar = 0f;
  [DefaultValue(0f)]
  public float minVegetation = 0f;
  [DefaultValue(1f)]
  public float maxVegetation = 1f;
  [DefaultValue(false)]
  public bool surroundCheckVegetation = false;
  [DefaultValue(20f)]
  public float surroundCheckDistance = 20f;
  [DefaultValue(2)]
  public int surroundCheckLayers = 2;
  [DefaultValue(0f)]
  public float surroundBetterThanAverage = 0f;
}
