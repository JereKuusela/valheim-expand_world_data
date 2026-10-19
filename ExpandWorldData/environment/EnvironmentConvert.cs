using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common;
using ExpandWorldData.Data;
using Service;
using UnityEngine;

namespace ExpandWorldData.Environment;

public partial class EnvironmentManager
{
  public static EnvSetup FromData(EnvironmentYaml data, string fileName)
  {
    EnvSetup env = new() { m_psystems = [] };
    if (Originals.TryGetValue(data.particles, out var setup))
      env = setup.Clone();
    else if (Originals.TryGetValue(data.name, out setup))
      env = setup.Clone();
    else
      Log.Warning($"{fileName}: Failed to find a particle system \"{data.particles}\" for environment {data.name}. Make sure field \"particles\" is set correctly or remove this entry.");

    env.m_name = data.name;
    env.m_default = data.isDefault;
    env.m_isWet = data.isWet;
    env.m_isFreezing = data.isFreezing;
    env.m_isFreezingAtNight = data.isFreezingAtNight;
    env.m_isCold = data.isCold;
    env.m_isColdAtNight = data.isColdAtNight;
    env.m_alwaysDark = data.alwaysDark;
    env.m_snowBuildup = data.snowBuildup;
    env.m_ambColorNight = data.ambColorNight ?? DataManager.ToColor(data.colorAmbientNight);
    env.m_ambColorDay = data.ambColorDay ?? DataManager.ToColor(data.colorAmbientDay);
    env.m_fogColorNight = data.fogColorNight ?? DataManager.ToColor(data.colorFogNight);
    env.m_fogColorMorning = data.fogColorMorning ?? DataManager.ToColor(data.colorFogMorning);
    env.m_fogColorDay = data.fogColorDay ?? DataManager.ToColor(data.colorFogDay);
    env.m_fogColorEvening = data.fogColorEvening ?? DataManager.ToColor(data.colorFogEvening);
    env.m_fogColorSunNight = data.fogColorSunNight ?? DataManager.ToColor(data.colorFogSunNight);
    env.m_fogColorSunMorning = data.fogColorSunMorning ?? DataManager.ToColor(data.colorFogSunMorning);
    env.m_fogColorSunDay = data.fogColorSunDay ?? DataManager.ToColor(data.colorFogSunDay);
    env.m_fogColorSunEvening = data.fogColorSunEvening ?? DataManager.ToColor(data.colorFogSunEvening);
    env.m_fogDensityNight = data.fogDensityNight;
    env.m_fogDensityMorning = data.fogDensityMorning;
    env.m_fogDensityDay = data.fogDensityDay;
    env.m_fogDensityEvening = data.fogDensityEvening;
    env.m_sunColorNight = data.sunColorNight ?? DataManager.ToColor(data.colorSunNight);
    env.m_sunColorMorning = data.sunColorMorning ?? DataManager.ToColor(data.colorSunMorning);
    env.m_sunColorDay = data.sunColorDay ?? DataManager.ToColor(data.colorSunDay);
    env.m_sunColorEvening = data.sunColorEvening ?? DataManager.ToColor(data.colorSunEvening);
    env.m_lightIntensityDay = data.lightIntensityDay;
    env.m_lightIntensityNight = data.lightIntensityNight;
    env.m_sunAngle = data.sunAngle;
    env.m_windMin = data.windMin;
    env.m_windMax = data.windMax;
    env.m_rainCloudAlpha = data.rainCloudAlpha;
    env.m_ambientVol = data.ambientVol;
    env.m_ambientList = data.ambientList;
    env.m_musicMorning = data.musicMorning;
    env.m_musicEvening = data.musicEvening;
    env.m_musicDay = data.musicDay;
    env.m_musicNight = data.musicNight;
    env.m_auroraIntensityNight = data.auroraIntensityNight;
    env.m_auroraIntensityMorning = data.auroraIntensityMorning;
    env.m_auroraIntensityDay = data.auroraIntensityDay;
    env.m_auroraIntensityEvening = data.auroraIntensityEvening;
    env.m_auroraColors = ToGradient(data.auroraColors, fileName);
    env.m_cloudOpacityNight = data.cloudOpacityNight;
    env.m_cloudOpacityMorning = data.cloudOpacityMorning;
    env.m_cloudOpacityDay = data.cloudOpacityDay;
    env.m_cloudOpacityEvening = data.cloudOpacityEvening;
    env.m_aoIntensityNight = data.aoIntensityNight;
    env.m_aoIntensityMorning = data.aoIntensityMorning;
    env.m_aoIntensityDay = data.aoIntensityDay;
    env.m_aoIntensityEvening = data.aoIntensityEvening;
    env.m_ambientOcclusionColor = DataManager.ToColor(data.colorAmbientOcclusion);
    env.m_psystemsOutsideOnly = data.psystemsOutsideOnly;

    EnvironmentData extra = new(data);
    if (extra.IsValid())
      Extra[data.name] = extra;
    return env;
  }

  public static EnvironmentYaml ToData(EnvSetup env) => new()
  {
    name = env.m_name,
    isDefault = env.m_default,
    isWet = env.m_isWet,
    isFreezing = env.m_isFreezing,
    isFreezingAtNight = env.m_isFreezingAtNight,
    isCold = env.m_isCold,
    isColdAtNight = env.m_isColdAtNight,
    alwaysDark = env.m_alwaysDark,
    snowBuildup = env.m_snowBuildup,
    colorAmbientDay = DataManager.FromColor(env.m_ambColorDay),
    colorAmbientNight = DataManager.FromColor(env.m_ambColorNight),
    colorFogNight = DataManager.FromColor(env.m_fogColorNight),
    colorFogMorning = DataManager.FromColor(env.m_fogColorMorning),
    colorFogDay = DataManager.FromColor(env.m_fogColorDay),
    colorFogEvening = DataManager.FromColor(env.m_fogColorEvening),
    colorFogSunNight = DataManager.FromColor(env.m_fogColorSunNight),
    colorFogSunMorning = DataManager.FromColor(env.m_fogColorSunMorning),
    colorFogSunDay = DataManager.FromColor(env.m_fogColorSunDay),
    colorFogSunEvening = DataManager.FromColor(env.m_fogColorSunEvening),
    fogDensityNight = env.m_fogDensityNight,
    fogDensityMorning = env.m_fogDensityMorning,
    fogDensityDay = env.m_fogDensityDay,
    fogDensityEvening = env.m_fogDensityEvening,
    colorSunNight = DataManager.FromColor(env.m_sunColorNight),
    colorSunMorning = DataManager.FromColor(env.m_sunColorMorning),
    colorSunDay = DataManager.FromColor(env.m_sunColorDay),
    colorSunEvening = DataManager.FromColor(env.m_sunColorEvening),
    lightIntensityDay = env.m_lightIntensityDay,
    lightIntensityNight = env.m_lightIntensityNight,
    sunAngle = env.m_sunAngle,
    windMin = env.m_windMin,
    windMax = env.m_windMax,
    rainCloudAlpha = env.m_rainCloudAlpha,
    ambientVol = env.m_ambientVol,
    ambientList = env.m_ambientList,
    musicMorning = env.m_musicMorning,
    musicEvening = env.m_musicEvening,
    musicDay = env.m_musicDay,
    musicNight = env.m_musicNight,
    auroraIntensityNight = env.m_auroraIntensityNight,
    auroraIntensityMorning = env.m_auroraIntensityMorning,
    auroraIntensityDay = env.m_auroraIntensityDay,
    auroraIntensityEvening = env.m_auroraIntensityEvening,
    auroraColors = FromGradient(env.m_auroraColors),
    cloudOpacityNight = env.m_cloudOpacityNight,
    cloudOpacityMorning = env.m_cloudOpacityMorning,
    cloudOpacityDay = env.m_cloudOpacityDay,
    cloudOpacityEvening = env.m_cloudOpacityEvening,
    aoIntensityNight = env.m_aoIntensityNight,
    aoIntensityMorning = env.m_aoIntensityMorning,
    aoIntensityDay = env.m_aoIntensityDay,
    aoIntensityEvening = env.m_aoIntensityEvening,
    colorAmbientOcclusion = DataManager.FromColor(env.m_ambientOcclusionColor),
    psystemsOutsideOnly = env.m_psystemsOutsideOnly
  };

  // Unity supports at most 8 color keys. A new gradient is required because cloned environments share the original reference.
  private static Gradient ToGradient(string[]? keys, string fileName)
  {
    var colorKeys = new List<GradientColorKey>();
    foreach (var key in keys ?? [])
    {
      if (colorKeys.Count >= 8)
      {
        Log.Warning($"{fileName}: auroraColors supports at most 8 keys, ignoring the rest.");
        break;
      }
      var kvp = Parse.Kvp(key);
      var color = DataManager.ToColor(kvp.Value);
      colorKeys.Add(new(color, Mathf.Clamp01(Parse.Float(kvp.Key))));
    }
    if (colorKeys.Count == 0)
      colorKeys.Add(new(Color.black, 0f));
    var gradient = new Gradient();
    // Alpha is ignored because the aurora texture is RGB only.
    gradient.SetKeys([.. colorKeys], [new GradientAlphaKey(1f, 0f)]);
    return gradient;
  }

  internal static string[]? FromGradient(Gradient? gradient)
  {
    if (gradient == null) return null;
    var keys = gradient.colorKeys.Select(key => $"{key.time.ToString("0.###", CultureInfo.InvariantCulture)}, {DataManager.FromColor(key.color)}").ToArray();
    return keys.Length == 0 ? null : keys;
  }
}
