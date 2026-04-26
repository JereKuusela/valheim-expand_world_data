using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace ExpandWorldData;

public class TerritoryYaml
{
  public string territory = "";
  [DefaultValue(false)]
  public bool noBuild = false;
  [DefaultValue(1f)]
  public float altitudeMultiplier = 1f;
  [DefaultValue(1f)]
  public float waterDepthMultiplier = 1f;
  [DefaultValue(-1000f)]
  public float minimumAltitude = -1000f;
  [DefaultValue(10000f)]
  public float maximumAltitude = 10000f;
  [DefaultValue(0.5f)]
  public float excessFactor = 0.5f;
  [DefaultValue(1f)]
  public float forestMultiplier = 1f;
  [DefaultValue(0f)]
  public float altitudeDelta = 0f;
  public string? colorMap = null;
  public string? colorTerrain = null;
  public string? colorWaterSurface = null;
  public string? colorWaterTop = null;
  public string? colorWaterBottom = null;
  public string? colorWaterShallow = null;
  [DefaultValue(null)]
  public StatusData[]? statusEffects;
}

public class TerritoryData
{
  public string name = "";
  public bool noBuild = false;
  public float altitudeMultiplier = 1f;
  public float waterDepthMultiplier = 1f;
  public float altitudeDelta = 0f;
  public float excessFactor = 0.5f;
  public float excessSign = 1f;
  public float minimumAltitude = -1000f;
  public float maximumAltitude = 10000f;
  public float forestMultiplier = 1f;
  public Color? colorMap = null;
  public Color? colorTerrain = null;
  public Color? colorWaterTop = null;
  public Color? colorWaterBottom = null;
  public Color? colorWaterShallow = null;
  public Color? colorWaterSurface = null;
  public List<Status> statusEffects = [];

  public TerritoryData(TerritoryYaml data)
  {
    name = data.territory;
    noBuild = data.noBuild;
    altitudeMultiplier = data.altitudeMultiplier;
    waterDepthMultiplier = data.waterDepthMultiplier;
    altitudeDelta = data.altitudeDelta;
    excessFactor = UnityEngine.Mathf.Abs(data.excessFactor);
    excessSign = UnityEngine.Mathf.Sign(data.excessFactor);
    minimumAltitude = data.minimumAltitude;
    maximumAltitude = data.maximumAltitude;
    forestMultiplier = data.forestMultiplier;
    colorTerrain = data.colorTerrain == null ? null : DataManager.ToColor(data.colorTerrain);
    colorMap = data.colorMap == null ? null : DataManager.ToColor(data.colorMap);
    colorWaterBottom = data.colorWaterBottom == null ? null : DataManager.ToColor(data.colorWaterBottom);
    colorWaterTop = data.colorWaterTop == null ? null : DataManager.ToColor(data.colorWaterTop);
    colorWaterShallow = data.colorWaterShallow == null ? null : DataManager.ToColor(data.colorWaterShallow);
    colorWaterSurface = data.colorWaterSurface == null ? null : DataManager.ToColor(data.colorWaterSurface);
    if (data.statusEffects != null)
      statusEffects = [.. data.statusEffects.Select(s => new Status(s))];
  }

  public bool IsValid() =>
    noBuild ||
    statusEffects.Count > 0 ||
    altitudeMultiplier != 1f ||
    waterDepthMultiplier != 1f ||
    altitudeDelta != 0f ||
    minimumAltitude != -1000f ||
    maximumAltitude != 10000f ||
    colorMap.HasValue ||
    colorTerrain.HasValue ||
    colorWaterTop.HasValue ||
    colorWaterBottom.HasValue ||
    colorWaterShallow.HasValue ||
    colorWaterSurface.HasValue ||
    forestMultiplier != 1f;
}
