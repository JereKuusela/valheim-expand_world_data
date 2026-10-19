using System;
using System.Collections.Generic;
using System.Linq;
using Service;
using Common;
using ExpandWorldData.Locations;
using ExpandWorldData.Vegetation;
using ExpandWorldData.Data;

namespace ExpandWorldData.Migration;

///<summary>Backfills altBiome for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
internal static class AltBiomeMigration
{
  public static void Apply()
  {
    var locations = MigrateLocations(Migrations.Read<LocationYaml>(LocationManager.Pattern), LocationManager.DefaultEntries, LocationManager.Pattern);
    Migrations.Finish(LocationManager.Pattern, locations);
    var vegetation = MigrateVegetation(Migrations.Read<VegetationYaml>(VegetationManager.Pattern), VegetationManager.DefaultEntries, VegetationManager.Pattern);
    Migrations.Finish(VegetationManager.Pattern, vegetation);
  }

  private static bool MigrateLocations(List<LocationYaml> data, List<ZoneSystem.ZoneLocation> defaults, string pattern) =>
    Migrate(data, defaults, yaml => yaml.prefab, yaml => yaml.biome, location => location.m_altBiomeParent, location => Parse.Name(location.m_prefab.Name), location => location.m_biome, pattern);

  private static bool MigrateVegetation(List<VegetationYaml> data, List<ZoneSystem.ZoneVegetation> defaults, string pattern) =>
    Migrate(data, defaults, yaml => yaml.prefab, yaml => yaml.biome, vegetation => vegetation.m_altBiomeParent, vegetation => Parse.Name(vegetation.m_prefab.name), vegetation => vegetation.m_biome, pattern);

  private static bool Migrate<TYaml, TDefault>(List<TYaml> data, List<TDefault> defaults, Func<TYaml, string> getPrefab, Func<TYaml, string> getBiome, Func<TDefault, string?> getDefaultAltBiome, Func<TDefault, string> getDefaultPrefab, Func<TDefault, Heightmap.Biome> getDefaultBiome, string pattern)
  {
    if (Yaml.HasField(pattern, "altBiome")) return false;
    // A prefab may have both ordinary and alternative-biome spawn rules. Never
    // discard its ordinary rules when deciding whether a restriction is safe.
    Dictionary<string, List<TDefault>> vanillaByPrefab = defaults
      .GroupBy(getDefaultPrefab)
      .ToDictionary(group => group.Key, group => group.ToList());
    Dictionary<string, string> migrations = [];
    foreach (IGrouping<string, TYaml> entries in data.GroupBy(getPrefab))
    {
      string prefab = entries.Key;
      if (!vanillaByPrefab.TryGetValue(Parse.Name(prefab), out List<TDefault>? candidates)) continue;
      string? value = getDefaultAltBiome(candidates[0]);
      if (value == null || value.Length == 0) continue;
      if (candidates.Any(item => getDefaultAltBiome(item) != value)) continue;
      Heightmap.Biome allowedBiomes = candidates.Aggregate((Heightmap.Biome)0, (biomes, item) => biomes | getDefaultBiome(item));
      // The YAML writer updates every entry with this prefab name. Skip the
      // entire group if even one custom rule allows a different base biome.
      if (entries.Any(item =>
      {
        var biome = DataManager.ToBiomes(getBiome(item), pattern);
        return biome == 0 || (biome & allowedBiomes) != biome;
      })) continue;
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
