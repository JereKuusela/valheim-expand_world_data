using System.Collections.Generic;
using System.ComponentModel;
using Service;
using Data;
using UnityEngine;

namespace ExpandWorldData;

public class VegetationData
{
  public string prefab = "";
  public bool enabled = true;
  public float min = 1f;
  public float max = 1f;
  [DefaultValue(false)]
  public bool forcePlacement = false;
  [DefaultValue("1")]
  public string scaleMin = "1";
  [DefaultValue("1")]
  public string scaleMax = "1";
  [DefaultValue(true)]
  public bool scaleUniform = true;
  [DefaultValue(0f)]
  public float randTilt = 0f;
  [DefaultValue(0f)]
  public float chanceToUseGroundTilt = 0f;

  [DefaultValue("")]
  public string biome = "";

  [DefaultValue("")]
  public string biomeArea = "";
  [DefaultValue(true)]
  public bool blockCheck = true;
  [DefaultValue(0f)]
  public float minAltitude = 0f;
  [DefaultValue(10000f)]
  public float maxAltitude = 10000f;
  [DefaultValue(0f)]
  public float minOceanDepth = 0f;
  [DefaultValue(0f)]
  public float maxOceanDepth = 0f;
  [DefaultValue(0f)]
  public float minVegetation = 0f;
  [DefaultValue(0f)]
  public float maxVegetation = 0f;
  [DefaultValue(0f)]
  public float minTilt = 0f;
  [DefaultValue(90f)]
  public float maxTilt = 90f;
  [DefaultValue(0f)]
  public float terrainDeltaRadius = 0f;
  [DefaultValue(0f)]
  public float minTerrainDelta = 0f;
  [DefaultValue(10f)]
  public float maxTerrainDelta = 10f;
  [DefaultValue(false)]
  public bool snapToWater = false;
  [DefaultValue(false)]
  public bool snapToStaticSolid = false;
  [DefaultValue(0f)]
  public float groundOffset = 0f;
  [DefaultValue(1)]
  public int groupSizeMin = 1;
  [DefaultValue(1)]
  public int groupSizeMax = 1;
  [DefaultValue(0f)]
  public float groupRadius = 0f;
  [DefaultValue(false)]
  public bool inForest = false;
  [DefaultValue(0f)]
  public float forestTresholdMin = 0f;
  [DefaultValue(1f)]
  public float forestTresholdMax = 1f;
  [DefaultValue(false)]
  public bool clearArea = false;
  [DefaultValue(0f)]
  public float clearRadius = 0f;
  [DefaultValue("")]
  public string requiredGlobalKey = "";
  [DefaultValue("")]
  public string forbiddenGlobalKey = "";
  [DefaultValue("")]
  public string data = "";
  [DefaultValue(false)]
  public bool surroundCheckVegetation = false;
  [DefaultValue(20f)]
  public float surroundCheckDistance = 20f;
  [DefaultValue(2)]
  public int surroundCheckLayers = 2;
  [DefaultValue(0f)]
  public float surroundBetterThanAverage = 0f;
  [DefaultValue(0f)]
  public float minDistance = 0f;
  [DefaultValue(0f)]
  public float maxDistance = 0f;
  [DefaultValue(0f)]
  public float centerX = 0f;
  [DefaultValue(0f)]
  public float centerY = 0f;
}

public class VegetationExtra
{
  public List<string>? requiredGlobalKeys;
  public List<string>? forbiddenGlobalKeys;
  public DataEntry? data;
  public Range<Vector3>? scale;
  public float clearRadius = 0;
  public bool clearArea = false;
  public Range<float>? distance;
  public Vector2? center;

  public bool IsDistanceOk(Vector3 pos)
  {
    if (distance == null) return true;
    var d = Vector3.Distance(pos, center ?? Vector3.zero);
    if (distance.Max == 0f) return distance.Min <= d;
    return distance.Min <= d && d <= distance.Max;
  }
  public bool IsValid() =>
    requiredGlobalKeys != null ||
    forbiddenGlobalKeys != null ||
    data != null ||
    scale != null ||
    clearRadius != 0f ||
    clearArea ||
    distance != null ||
    center != null;
}