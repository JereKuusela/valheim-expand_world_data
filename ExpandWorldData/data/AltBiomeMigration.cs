using System;
using System.Collections.Generic;
using System.Linq;
using Service;

namespace ExpandWorldData;

///<summary>Backfills altBiome for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
internal static class AltBiomeMigration
{
  public static bool MigrateLocations(List<ZoneSystem.ZoneLocation> data, List<ZoneSystem.ZoneLocation> defaults, string pattern) =>
    Migrate(data, defaults, location => location.m_prefab.Name, location => location.m_altBiomeParent, location => Parse.Name(location.m_prefab.Name), pattern);

  public static bool MigrateVegetation(List<ZoneSystem.ZoneVegetation> data, List<ZoneSystem.ZoneVegetation> defaults, string pattern) =>
    Migrate(data, defaults, vegetation => vegetation.m_prefab.name, vegetation => vegetation.m_altBiomeParent, vegetation => Parse.Name(vegetation.m_prefab.name), pattern);

  private static bool Migrate<T>(List<T> data, List<T> defaults, Func<T, string> getPrefab, Func<T, string?> getAltBiome, Func<T, string> getDefaultPrefab, string pattern)
  {
    // If any entry already has altBiome set, assume the file is up to date.
    if (data.Any(item => getAltBiome(item) != null)) return false;
    var vanillaAltBiomeByPrefab = defaults
      .Where(item => !string.IsNullOrEmpty(getAltBiome(item)))
      .GroupBy(getDefaultPrefab)
      .ToDictionary(group => group.Key, group => getAltBiome(group.First())!);
    Dictionary<string, string> migrations = [];
    foreach (var item in data)
    {
      var prefab = getPrefab(item);
      if (migrations.ContainsKey(prefab)) continue;
      if (!vanillaAltBiomeByPrefab.TryGetValue(Parse.Name(prefab), out var value)) continue;
      if (string.IsNullOrEmpty(value)) continue;
      migrations[prefab] = value;
    }
    if (migrations.Count == 0) return false;
    Log.Warning($"Prepared {migrations.Count} altBiome migrations for files matching {pattern}.");
    var changed = Yaml.InsertMissingField(pattern, "prefab", "altBiome", migrations);
    if (changed)
    {
      Log.Warning($"Adding {migrations.Count} missing alternative biomes to {pattern} files.");
      foreach (var kvp in migrations)
        Log.Warning($"Adding alternative biome {kvp.Value} to prefab {kvp.Key}.");
    }
    return changed;
  }
}
