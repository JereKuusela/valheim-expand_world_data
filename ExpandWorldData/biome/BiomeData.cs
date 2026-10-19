using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ExpandWorldData.Data;
using Service;
using UnityEngine;

namespace ExpandWorldData.Biome;

public class BiomeEnvironment
{
  public string environment = "";
  [DefaultValue(1f)]
  public float weight = 1f;
  [DefaultValue(false)]
  public bool ashlandsOverride;
  [DefaultValue(false)]
  public bool deepNorthOverride;
  [DefaultValue(null)]
  public StringList? requiredGlobalKeys = null;
  [DefaultValue(null)]
  public StringList? forbiddenGlobalKeys = null;
  [DefaultValue(null)]
  public StringList? requiredPlayerKeys = null;
  [DefaultValue(null)]
  public StringList? forbiddenPlayerKeys = null;

}

public class BiomeYaml
{
  public string biome = "";
  [DefaultValue("")]
  public string name = "";
  [DefaultValue("")]
  public string terrain = "";
  [DefaultValue("")]
  public string nature = "";
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
  [DefaultValue(null)]
  public BiomeEnvironment[]? environments = null;
  [DefaultValue("")]
  public string paint = "";
  public Color? color = null;
  [DefaultValue(1f)]
  public float mapColorMultiplier = 1f;
  public Color? mapColor = null;
  public string? colorMap = null;
  public string? colorTerrain = null;
  public string? colorWaterSurface = null;
  public string? colorWaterTop = null;
  public string? colorWaterBottom = null;
  public string? colorWaterShallow = null;
  [DefaultValue("")]
  public string musicMorning = "morning";
  [DefaultValue("")]
  public string musicEvening = "evening";
  [DefaultValue("")]
  public string musicDay = "";
  [DefaultValue("")]
  public string musicNight = "";
  [DefaultValue(false)]
  public bool noBuild = false;
  [DefaultValue(null)]
  public StatusData[]? statusEffects;
  [DefaultValue("")]
  public string lava = "";
  [DefaultValue(1f)]
  public float lavaAmount = 1f;
  [DefaultValue(1f)]
  public float lavaStretch = 1f;
}

public class BiomeData
{
  public bool noBuild = false;
  public float altitudeMultiplier = 1f;
  public float waterDepthMultiplier = 1f;
  public float altitudeDelta = 0f;
  public float excessFactor = 0.5f;
  public float excessSign = 1f;
  public float minimumAltitude = -1000f;
  public float maximumAltitude = 10000f;
  public float mapColorMultiplier = 1f;
  public Color colorTerrain = new(0, 0, 0, 0);
  public Color colorMap = new(0, 0, 0, 0);
  public Color? colorWaterTop = null;
  public Color? colorWaterBottom = null;
  public Color? colorWaterShallow = null;
  public Color? colorWaterSurface = null;
  public float forestMultiplier = 1f;
  public bool lava = false;
  public float lavaAmount = 1f;
  public float lavaStretch = 1f;
  private float? lavaSeed = null;
  public float GetLavaSeed()
  {
    if (lavaSeed == null)
    {
      Random.State state = Random.state;
      Random.InitState(ZNet.m_world.m_seed);
      lavaSeed = Random.Range(-10000, 10000);
      Random.state = state;
    }
    return lavaSeed.Value;
  }

  public List<Status> statusEffects = [];

  public BiomeData(BiomeYaml data)
  {
    noBuild = data.noBuild;
    altitudeMultiplier = data.altitudeMultiplier;
    waterDepthMultiplier = data.waterDepthMultiplier;
    altitudeDelta = data.altitudeDelta;
    excessFactor = Mathf.Abs(data.excessFactor);
    excessSign = Mathf.Sign(data.excessFactor);
    minimumAltitude = data.minimumAltitude;
    maximumAltitude = data.maximumAltitude;
    mapColorMultiplier = data.mapColorMultiplier;
    colorTerrain = data.color ?? DataManager.ToColor(data.colorTerrain);
    colorMap = data.mapColor ?? DataManager.ToColor(data.colorMap);
    colorWaterBottom = data.colorWaterBottom == null ? null : DataManager.ToColor(data.colorWaterBottom);
    colorWaterTop = data.colorWaterTop == null ? null : DataManager.ToColor(data.colorWaterTop);
    colorWaterShallow = data.colorWaterShallow == null ? null : DataManager.ToColor(data.colorWaterShallow);
    colorWaterSurface = data.colorWaterSurface == null ? null : DataManager.ToColor(data.colorWaterSurface);
    forestMultiplier = data.forestMultiplier;
    if (data.statusEffects != null)
      statusEffects = data.statusEffects.Select(s => new Status(s)).ToList();
    // Lava only visually appears on Ashlands terrain, so that can be used as the default.
    if (data.lava == "")
      lava = colorTerrain.a > 0.929f && colorTerrain.r > 0.929f;
    else
      lava = data.lava == "true";
    lavaAmount = data.lavaAmount;
    lavaStretch = 0.01f / data.lavaStretch;
  }
  public bool IsValid() =>
    statusEffects.Count > 0 ||
    altitudeMultiplier != 1f ||
    waterDepthMultiplier != 1f ||
    altitudeDelta != 0f ||
    minimumAltitude != -1000f ||
    maximumAltitude != 10000f ||
    mapColorMultiplier != 1f ||
    colorMap.a != 0 ||
    forestMultiplier != 1f ||
    lavaAmount != 1f;
}


public class EnvEntryKeys(BiomeEnvironment data)
{
  public List<string[]> requiredGlobalKeys = StringList.ToRows(data.requiredGlobalKeys) ?? [];
  public List<string[]> forbiddenGlobalKeys = StringList.ToRows(data.forbiddenGlobalKeys) ?? [];
  public List<string[]> requiredPlayerKeys = StringList.ToRows(data.requiredPlayerKeys) ?? [];
  public List<string[]> forbiddenPlayerKeys = StringList.ToRows(data.forbiddenPlayerKeys) ?? [];

  public bool CheckKeys()
  {
    if (!StringList.Pass(requiredGlobalKeys, ZoneSystem.instance.GetGlobalKey)) return false;
    if (StringList.Forbids(forbiddenGlobalKeys, ZoneSystem.instance.GetGlobalKey)) return false;
    var player = Player.m_localPlayer;
    if (player && !StringList.Pass(requiredPlayerKeys, player.HaveUniqueKey)) return false;
    if (player && StringList.Forbids(forbiddenPlayerKeys, player.HaveUniqueKey)) return false;
    return true;
  }
  public bool HasKeys() => requiredGlobalKeys.Count > 0 || forbiddenGlobalKeys.Count > 0 || requiredPlayerKeys.Count > 0 || forbiddenPlayerKeys.Count > 0;
}