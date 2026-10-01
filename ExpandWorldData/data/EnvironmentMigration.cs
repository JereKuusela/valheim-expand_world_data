using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorldData;

///<summary>Backfills environment fields added after the yaml was saved, so that omitted values (which mean off) don't change the look.</summary>
internal static class EnvironmentMigration
{
  private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

  private static bool IsBlack(Gradient? gradient) => gradient == null || gradient.colorKeys.All(key => key.color.r == 0f && key.color.g == 0f && key.color.b == 0f);

  // Field, vanilla value getter, value that is used when the field is omitted.
  private static readonly (string Field, Func<EnvSetup, string> Get, string Default)[] Fields =
  [
    ("snowBuildup", e => F(e.m_snowBuildup), "0"),
    ("auroraIntensityNight", e => F(e.m_auroraIntensityNight), "0"),
    ("auroraIntensityMorning", e => F(e.m_auroraIntensityMorning), "0"),
    ("auroraIntensityDay", e => F(e.m_auroraIntensityDay), "0"),
    ("auroraIntensityEvening", e => F(e.m_auroraIntensityEvening), "0"),
    ("auroraColors", e => IsBlack(e.m_auroraColors) ? "" : $"[{string.Join(", ", EnvironmentManager.FromGradient(e.m_auroraColors)!.Select(k => $"\"{k}\""))}]", ""),
    ("cloudOpacityNight", e => F(e.m_cloudOpacityNight), "8.53"),
    ("cloudOpacityMorning", e => F(e.m_cloudOpacityMorning), "8.53"),
    ("cloudOpacityDay", e => F(e.m_cloudOpacityDay), "8.53"),
    ("cloudOpacityEvening", e => F(e.m_cloudOpacityEvening), "8.53"),
    ("aoIntensityNight", e => F(e.m_aoIntensityNight), "1"),
    ("aoIntensityMorning", e => F(e.m_aoIntensityMorning), "0.85"),
    ("aoIntensityDay", e => F(e.m_aoIntensityDay), "0.8"),
    ("aoIntensityEvening", e => F(e.m_aoIntensityEvening), "0.85"),
    ("colorAmbientOcclusion", e => DataManager.FromColor(e.m_ambientOcclusionColor), "0.005, 0, 0.226"),
    ("psystemsOutsideOnly", e => e.m_psystemsOutsideOnly ? "true" : "false", "false"),
  ];

  ///<summary>Returns true if any file was changed.</summary>
  public static bool Migrate(List<EnvironmentYaml> yamls, Dictionary<string, EnvSetup> originals, string pattern)
  {
    var changed = false;
    foreach (var (field, get, def) in Fields)
    {
      if (Yaml.HasField(pattern, field)) continue;
      Dictionary<string, string> migrations = [];
      foreach (var yaml in yamls)
      {
        // Same source lookup as EnvironmentManager.FromData: cloned environments inherited these values.
        if (!originals.TryGetValue(yaml.particles, out var source) && !originals.TryGetValue(yaml.name, out source)) continue;
        var value = get(source);
        if (value == def) continue;
        migrations[yaml.name] = value;
      }
      if (migrations.Count == 0) continue;
      if (!Yaml.InsertMissingField(pattern, "name", field, migrations)) continue;
      changed = true;
      Log.Warning($"Added {field} to {pattern} files.");
    }
    return changed;
  }
}
