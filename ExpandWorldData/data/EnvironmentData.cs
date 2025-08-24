using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace ExpandWorldData;

public class EnvironmentYaml
{
  public string name = "";
  [DefaultValue("")]
  public string particles = "";
  [DefaultValue(false)]
  public bool isDefault = false;
  [DefaultValue(false)]
  public bool isWet = false;
  [DefaultValue(false)]
  public bool isFreezing = false;
  [DefaultValue(false)]
  public bool isFreezingAtNight = false;
  [DefaultValue(false)]
  public bool isCold = false;
  [DefaultValue(false)]
  public bool isColdAtNight = false;
  [DefaultValue(false)]
  public bool alwaysDark = false;
  [DefaultValue(0f)]
  public float windMin = 0f;
  [DefaultValue(1f)]
  public float windMax = 1f;
  [DefaultValue(0f)]
  public float rainCloudAlpha = 0f;
  [DefaultValue(0.3f)]
  public float ambientVol = 0.3f;
  [DefaultValue("")]
  public string ambientList = "";
  [DefaultValue("")]
  public string musicMorning = "";
  [DefaultValue("")]
  public string musicEvening = "";
  [DefaultValue("")]
  public string musicDay = "";
  [DefaultValue("")]
  public string musicNight = "";
  public string? colorAmbientDay;
  public string? colorAmbientNight;
  public string? colorSunMorning;
  public string? colorSunDay;
  public string? colorSunEvening;
  public string? colorSunNight;
  public string? colorFogMorning;
  public string? colorFogDay;
  public string? colorFogEvening;
  public string? colorFogNight;
  public string? colorFogSunMorning;
  public string? colorFogSunDay;
  public string? colorFogSunEvening;
  public string? colorFogSunNight;
  public Color? ambColorDay;
  public Color? ambColorNight;
  public Color? sunColorMorning;
  public Color? sunColorDay;
  public Color? sunColorEvening;
  public Color? sunColorNight;
  public Color? fogColorMorning;
  public Color? fogColorDay;
  public Color? fogColorEvening;
  public Color? fogColorNight;
  public Color? fogColorSunMorning;
  public Color? fogColorSunDay;
  public Color? fogColorSunEvening;
  public Color? fogColorSunNight;
  [DefaultValue(0.01f)]
  public float fogDensityMorning = 0.01f;
  [DefaultValue(0.01f)]
  public float fogDensityDay = 0.01f;
  [DefaultValue(0.01f)]
  public float fogDensityEvening = 0.01f;
  [DefaultValue(0.01f)]
  public float fogDensityNight = 0.01f;
  [DefaultValue(1.2f)]
  public float lightIntensityDay = 1.2f;
  [DefaultValue(0f)]
  public float lightIntensityNight = 0f;
  [DefaultValue(60f)]
  public float sunAngle = 60f;
  [DefaultValue(null)]
  public StatusData[]? statusEffects;
}

public class EnvironmentData
{
  public List<Status> statusEffects = [];
  public List<string> requiredGlobalKeys = [];
  public List<string> forbiddenGlobalKeys = [];
  public List<string> requiredPlayerKeys = [];
  public List<string> forbiddenPlayerKeys = [];
  public EnvironmentData(EnvironmentYaml data)
  {
    if (data.statusEffects != null)
      statusEffects = data.statusEffects.Select(s => new Status(s)).ToList();
  }
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
  public bool IsValid() => statusEffects.Count > 0 || HasKeys();
}