using System;
using System.Collections.Generic;
using System.Linq;
using Service;
using Common;

namespace ExpandWorldData;

///<summary>Backfills altBiome for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
internal static class AltBiomeMigration
{
  public static bool MigrateLocations(List<ZoneSystem.ZoneLocation> data, List<ZoneSystem.ZoneLocation> defaults, string pattern) =>
    Migrate(data, defaults, location => location.m_prefab.Name, location => location.m_altBiomeParent, location => Parse.Name(location.m_prefab.Name), location => location.m_biome, pattern);

  public static bool MigrateVegetation(List<ZoneSystem.ZoneVegetation> data, List<ZoneSystem.ZoneVegetation> defaults, string pattern) =>
    Migrate(data, defaults, vegetation => vegetation.m_prefab.name, vegetation => vegetation.m_altBiomeParent, vegetation => Parse.Name(vegetation.m_prefab.name), vegetation => vegetation.m_biome, pattern);

  private static bool Migrate<T>(List<T> data, List<T> defaults, Func<T, string> getPrefab, Func<T, string?> getAltBiome, Func<T, string> getDefaultPrefab, Func<T, Heightmap.Biome> getBiome, string pattern)
  {
    // If any entry already has altBiome set, assume the file is up to date.
    if (data.Any(item => getAltBiome(item) != null)) return false;
    // A prefab may have both ordinary and alternative-biome spawn rules. Never
    // discard its ordinary rules when deciding whether a restriction is safe.
    Dictionary<string, List<T>> vanillaByPrefab = defaults
      .GroupBy(getDefaultPrefab)
      .ToDictionary(group => group.Key, group => group.ToList());
    Dictionary<string, string> migrations = [];
    foreach (IGrouping<string, T> entries in data.GroupBy(getPrefab))
    {
      string prefab = entries.Key;
      if (!vanillaByPrefab.TryGetValue(Parse.Name(prefab), out List<T>? candidates)) continue;
      string? value = getAltBiome(candidates[0]);
      if (string.IsNullOrEmpty(value)) continue;
      if (candidates.Any(item => getAltBiome(item) != value)) continue;
      Heightmap.Biome allowedBiomes = candidates.Aggregate((Heightmap.Biome)0, (biomes, item) => biomes | getBiome(item));
      // The YAML writer updates every entry with this prefab name. Skip the
      // entire group if even one custom rule allows a different base biome.
      if (entries.Any(item => getBiome(item) == 0 || (getBiome(item) & allowedBiomes) != getBiome(item))) continue;
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
