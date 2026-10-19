
using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData.Biome;
using ExpandWorldData.Clutter;
using ExpandWorldData.Compatibility;
using ExpandWorldData.Dungeon;
using ExpandWorldData.Locations;
using ExpandWorldData.Territory;
using ExpandWorldData.Vegetation;
using ExpandWorldData.World;
using UnityEngine;

namespace ExpandWorldData;

public static class Api
{
  public static RandomEvent? GetCurrentRandomEvent(Vector3 position)
  {
    if (Configuration.DataEvents && Configuration.MultipleEvents)
      return Event.MultipleEvents.Events.OrderBy(entry => Utils.DistanceXZ(entry.Event.m_pos, position)).FirstOrDefault()?.Event;
    return RandEventSystem.instance?.GetCurrentRandomEvent();
  }

  public static float GetMinimapHeight(float height, Heightmap.Biome biome)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return height;
    if (height < WorldInfo.WaterLevel) return height;
    return (height - WorldInfo.WaterLevel) * data.mapColorMultiplier + WorldInfo.WaterLevel;
  }
  public static void AddBiome(Biome.BiomeYaml data)
  {
    BiomeManager.AddBiome(data);
  }
  public static void AddTerritory(Territory.TerritoryYaml data)
  {
    TerritoryManager.AddTerritory(data);
  }
  public static void AddClutter(Clutter.ClutterYaml data)
  {
    ClutterManager.AddClutter(data);
  }
  public static void AddDungeon(Dungeon.DungeonYaml data)
  {
    DungeonManager.AddDungeon(data);
  }
  public static void AddLocation(Locations.LocationYaml data)
  {
    LocationManager.AddLocation(data);
  }
  public static void AddRoom(Dungeon.RoomYaml data)
  {
    RoomManager.AddRoom(data);
  }
  public static void AddVegetation(Vegetation.VegetationYaml data)
  {
    VegetationManager.AddVegetation(data);
  }
  public static void ChangeWorld(World.WorldYaml data, int index)
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