using System.ComponentModel;
namespace ExpandWorldData;
public class ClutterData
{
  public string prefab = "";
  [DefaultValue(true)]
  public bool enabled = true;
  public int amount = 80;
  [DefaultValue("")]
  public string biome = "";
  [DefaultValue(false)]
  public bool instanced = false;
  [DefaultValue(true)]
  public bool onUncleared = true;
  [DefaultValue(false)]
  public bool onCleared = false;
  [DefaultValue(1f)]
  public float scaleMin = 1f;
  [DefaultValue(1f)]
  public float scaleMax = 1f;
  [DefaultValue(0f)]
  public float minTilt = 0f;
  [DefaultValue(10f)]
  public float maxTilt = 10f;
  [DefaultValue(-1000f)]
  public float minAltitude = -1000f;
  [DefaultValue(10000f)]
  public float maxAltitude = 10000f;
  [DefaultValue(0f)]
  public float minVegetation = 0f;
  [DefaultValue(0f)]
  public float maxVegetation = 0f;
  [DefaultValue(false)]
  public bool snapToWater = false;
  [DefaultValue(false)]
  public bool terrainTilt = false;
  [DefaultValue(0f)]
  public float randomOffset = 0f;
  [DefaultValue(0f)]
  public float minOceanDepth = 0f;
  [DefaultValue(0f)]
  public float maxOceanDepth = 0f;
  [DefaultValue(false)]
  public bool inForest = false;
  [DefaultValue(0f)]
  public float forestTresholdMin = 0f;
  [DefaultValue(1f)]
  public float forestTresholdMax = 1f;
  [DefaultValue(0f)]
  public float fractalScale = 0f;
  [DefaultValue(0f)]
  public float fractalOffset = 0f;
  [DefaultValue(0f)]
  public float fractalThresholdMin = 0f;
  [DefaultValue(1f)]
  public float fractalThresholdMax = 1f;
}
