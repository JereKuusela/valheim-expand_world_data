using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
namespace ExpandWorldData;

public class BiomeEnvironment
{
  public string environment = "";
  [DefaultValue(1f)]
  public float weight = 1f;
  [DefaultValue(false)]
  public bool? ashlandsOverride;
  [DefaultValue(false)]
  public bool? deepNorthOverride;
  [DefaultValue("")]
  public string requiredGlobalKeys = "";
  [DefaultValue("")]
  public string forbiddenGlobalKeys = "";
  [DefaultValue("")]
  public string requiredPlayerKeys = "";
  [DefaultValue("")]
  public string forbiddenPlayerKeys = "";

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
  public BiomeEnvironment[] environments = [];
  [DefaultValue("")]
  public string paint = "";
  public Color color = new(0, 0, 0, 0);
  [DefaultValue(1f)]
  public float mapColorMultiplier = 1f;
  public Color mapColor = Color.black;
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
  public Color color = new(0, 0, 0, 0);
  public Color mapColor = new(0, 0, 0, 0);
  public float forestMultiplier = 1f;
  public bool lava = false;

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
    color = data.color;
    mapColor = data.mapColor;
    forestMultiplier = data.forestMultiplier;
    if (data.statusEffects != null)
      statusEffects = data.statusEffects.Select(s => new Status(s)).ToList();
    // Lava only visually appears on Ashlands terrain, so that can be used as the default.
    if (data.lava == "")
      lava = data.color.a > 0.959f && data.color.r > 0.959f;
    else
      lava = data.lava == "true";
  }
  public bool IsValid() =>
    statusEffects.Count > 0 ||
    altitudeMultiplier != 1f ||
    waterDepthMultiplier != 1f ||
    altitudeDelta != 0f ||
    minimumAltitude != -1000f ||
    maximumAltitude != 10000f ||
    mapColorMultiplier != 1f ||
    mapColor.a != 0 ||
    forestMultiplier != 1f;
}


public class EnvEntryKeys(BiomeEnvironment data)
{
  public List<string> requiredGlobalKeys = DataManager.ToList(data.requiredGlobalKeys);
  public List<string> forbiddenGlobalKeys = DataManager.ToList(data.forbiddenGlobalKeys);
  public List<string> requiredPlayerKeys = DataManager.ToList(data.requiredPlayerKeys);
  public List<string> forbiddenPlayerKeys = DataManager.ToList(data.forbiddenPlayerKeys);

  public bool CheckKeys()
  {
    if (requiredGlobalKeys.Any(k => !ZoneSystem.instance.GetGlobalKey(k))) return false;
    if (forbiddenGlobalKeys.Any(ZoneSystem.instance.GetGlobalKey)) return false;
    var player = Player.m_localPlayer;
    if (player && requiredPlayerKeys.Any(k => !player.HaveUniqueKey(k))) return false;
    if (player && forbiddenPlayerKeys.Any(player.HaveUniqueKey)) return false;
    return true;
  }
  public bool HasKeys() => requiredGlobalKeys.Count > 0 || forbiddenGlobalKeys.Count > 0 || requiredPlayerKeys.Count > 0 || forbiddenPlayerKeys.Count > 0;
}