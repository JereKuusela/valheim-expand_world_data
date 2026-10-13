using System;
using System.Collections.Generic;
using ExpandWorldData;
using Service;

internal static class Program
{
  private static ZoneSystem.ZoneVegetation Rule(string prefab, Heightmap.Biome biome, string? alt = null) =>
    new() { m_prefab = new() { name = prefab }, m_biome = biome, m_altBiomeParent = alt };

  private static void Check(string name, List<ZoneSystem.ZoneVegetation> data, List<ZoneSystem.ZoneVegetation> defaults, string? expected)
  {
    Yaml.Writes.Clear();
    bool changed = AltBiomeMigration.MigrateVegetation(data, defaults, "expand_vegetation*.yaml");
    if (changed != (expected != null) || (expected != null && (!Yaml.Writes.TryGetValue(data[0].m_prefab.name, out string? actual) || actual != expected)))
      throw new Exception(name + " failed");
    Console.WriteLine("PASS " + name);
  }

  public static void Main()
  {
    Heightmap.Biome forest = Heightmap.Biome.BlackForest;
    Heightmap.Biome swamp = Heightmap.Biome.Swamp;
    ZoneSystem.ZoneVegetation ordinary = Rule("FirTree", forest);
    ZoneSystem.ZoneVegetation restricted = Rule("FirTree", forest, "Rock Black Forest");
    Check("ordinary rule before restricted rule", [ordinary], [ordinary, restricted], null);
    Check("ordinary rule after restricted rule", [ordinary], [restricted, ordinary], null);
    Check("different alternative biomes are ambiguous", [ordinary], [restricted, Rule("FirTree", forest, "Other Forest")], null);
    Check("exclusive alternative biome still migrates", [ordinary], [restricted], "Rock Black Forest");
    Check("custom biome remains unrestricted", [Rule("FirTree", swamp)], [restricted], null);
    Check("mixed biome mask remains unrestricted", [Rule("FirTree", forest | swamp)], [restricted], null);
    Check("duplicate prefab with incompatible biome prevents all writes", [ordinary, Rule("FirTree", swamp)], [restricted], null);
    Check("compatible duplicate prefab rules migrate", [ordinary, ordinary], [restricted], "Rock Black Forest");
    Check("explicit restriction remains unchanged", [Rule("FirTree", forest, "Custom Forest")], [restricted], null);
    Check("unknown prefab remains unchanged", [Rule("CustomTree", forest)], [restricted], null);
    Check("empty default restriction remains unrestricted", [ordinary], [Rule("FirTree", forest, ""), restricted], null);
    Check("empty biome remains unrestricted", [Rule("FirTree", 0)], [restricted], null);

    Yaml.Writes.Clear();
    ZoneSystem.ZoneLocation location = new() { m_prefab = new() { Name = "Tower" }, m_biome = forest };
    ZoneSystem.ZoneLocation altLocation = new() { m_prefab = new() { Name = "Tower" }, m_biome = forest, m_altBiomeParent = "Rock Black Forest" };
    if (AltBiomeMigration.MigrateLocations([location], [altLocation, location], "expand_locations*.yaml"))
      throw new Exception("ordinary location restricted");
    if (!AltBiomeMigration.MigrateLocations([location], [altLocation], "expand_locations*.yaml"))
      throw new Exception("exclusive location not migrated");
    Console.WriteLine("PASS shared location migration");
  }
}

// Only the game and file-writing boundary are replaced. The tests compile the
// production migration source above, without requiring Unity to run.
public class Heightmap
{
  [Flags] public enum Biome { BlackForest = 8, Swamp = 2 }
}
public class ZoneSystem
{
  public class Prefab { public string name = ""; public string Name = ""; }
  public class ZoneVegetation { public Prefab m_prefab = new(); public string? m_altBiomeParent; public Heightmap.Biome m_biome; }
  public class ZoneLocation { public Prefab m_prefab = new(); public string? m_altBiomeParent; public Heightmap.Biome m_biome; }
}
namespace Common { public static class Parse { public static string Name(string value) => value.Split(':')[0]; } }
namespace Service
{
  public static class Log { public static void Warning(string message) { } }
  public static class Yaml
  {
    public static readonly Dictionary<string, string> Writes = new();
    public static bool InsertMissingField(string pattern, string key, string field, Dictionary<string, string> values)
    {
      foreach (KeyValuePair<string, string> value in values) Writes.Add(value.Key, value.Value);
      return values.Count > 0;
    }
  }
}
