
using System;
using System.Collections.Generic;

namespace ExpandWorldData;

public static class Api
{
  public static float GetMinimapHeight(float height, Heightmap.Biome biome)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return height;
    if (height < WorldInfo.WaterLevel) return height;
    return (height - WorldInfo.WaterLevel) * data.mapColorMultiplier + WorldInfo.WaterLevel;
  }
  public static void AddBiome(BiomeYaml data)
  {
    BiomeManager.AddBiome(data);
  }
  public static void AddTerritory(TerritoryYaml data)
  {
    TerritoryManager.AddTerritory(data);
  }
  public static void AddClutter(ClutterYaml data)
  {
    ClutterManager.AddClutter(data);
  }
  public static void AddDungeon(DungeonYaml data)
  {
    Dungeon.Loader.AddDungeon(data);
  }
  public static void AddLocation(LocationYaml data)
  {
    LocationLoading.AddLocation(data);
  }
  public static void AddRoom(RoomYaml data)
  {
    RoomLoading.AddRoom(data);
  }
  public static void AddVegetation(VegetationYaml data)
  {
    VegetationLoading.AddVegetation(data);
  }
  public static void ChangeWorld(WorldYaml data, int index)
  {
    WorldManager.AddWorld(data, index);
  }

  // Returns all groups that the location belongs to.
  public static HashSet<string> GetLocationGroups(ZoneSystem.ZoneLocation location)
  {
    if (!LocationExtra.TryGet(location, out var data)) return [];
    return data.Groups;
  }
  // Returns minimum distance rules for the location, or null when vanilla logic is used.
  public static List<Tuple<string, float>>? GetLocationAwayFrom(ZoneSystem.ZoneLocation location)
  {
    if (!LocationExtra.TryGet(location, out var data)) return null;
    return data.AwayFrom;
  }
  // Returns maximum distance rules for the location, or null when vanilla logic is used.
  public static List<Tuple<string, float>>? GetLocationCloseTo(ZoneSystem.ZoneLocation location)
  {
    if (!LocationExtra.TryGet(location, out var data)) return null;
    return data.CloseTo;
  }
}