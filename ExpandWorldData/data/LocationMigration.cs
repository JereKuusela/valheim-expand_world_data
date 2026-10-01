using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service;

namespace ExpandWorldData;

///<summary>Backfills location fields added after the yaml was saved, so that omitted values (which mean off) don't change behavior.</summary>
internal static class LocationMigration
{
  // Field, value getter, value that is used when the field is omitted.
  private static readonly (string Field, Func<LocationYaml, string> Get, string Default)[] Fields =
  [
    ("interiorRadius", d => d.interiorRadius.ToString("0.####", NumberFormatInfo.InvariantInfo), "0"),
    ("interiorEnvironment", d => d.interiorEnvironment, ""),
    ("enemyMinLevel", d => d.enemyMinLevel.ToString(NumberFormatInfo.InvariantInfo), "-1"),
    ("enemyMaxLevel", d => d.enemyMaxLevel.ToString(NumberFormatInfo.InvariantInfo), "-1"),
    ("enemyLevelUpChance", d => d.enemyLevelUpChance.ToString("0.####", NumberFormatInfo.InvariantInfo), "-1"),
    ("enemyLevelExcludeGroups", d => d.enemyLevelExcludeGroups, ""),
    ("blockSpawnGroups", d => d.blockSpawnGroups, ""),
  ];

  ///<summary>Returns true if any file was changed.</summary>
  public static bool Migrate(List<ZoneSystem.ZoneLocation> data, Dictionary<string, ZoneSystem.ZoneLocation> originals, string pattern)
  {
    // One-shot, otherwise intentionally removed values would be restored.
    if (Fields.Any(field => Yaml.HasField(pattern, field.Field))) return false;
    Dictionary<string, LocationYaml> vanilla = [];
    foreach (var loc in data)
    {
      var name = loc.m_prefab.Name;
      if (vanilla.ContainsKey(name)) continue;
      // Blueprints are not found.
      if (!originals.TryGetValue(Parse.Name(name), out var original) || !original.m_prefab.IsValid) continue;
      var asset = Helper.SafeLoad(original);
      if (asset == null) continue;
      var prefab = asset.GetComponent<Location>();
      if (prefab)
      {
        LocationYaml yaml = new();
        LocationManager.FillClientFields(yaml, prefab);
        vanilla[name] = yaml;
      }
      original.m_prefab.Release();
    }
    var changed = false;
    foreach (var (field, get, def) in Fields)
    {
      var migrations = vanilla.Select(kvp => (kvp.Key, Value: get(kvp.Value))).Where(x => x.Value != def).ToDictionary(x => x.Key, x => x.Value);
      if (migrations.Count == 0) continue;
      if (!Yaml.InsertMissingField(pattern, "prefab", field, migrations)) continue;
      changed = true;
      Log.Warning($"Added {field} to {pattern} files.");
    }
    // Omitting noBuild used to keep the prefab value, now it disables it.
    if (changed)
    {
      var noBuilds = vanilla.Where(kvp => kvp.Value.noBuild != "").ToDictionary(kvp => kvp.Key, kvp => kvp.Value.noBuild);
      if (noBuilds.Count > 0 && Yaml.InsertMissingField(pattern, "prefab", "noBuild", noBuilds))
        Log.Warning($"Added noBuild to {pattern} files.");
    }
    return changed;
  }
}
