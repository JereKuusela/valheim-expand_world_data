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
    /**
     Read-side accessor for a location's similarity groups. Exposed on Api so other mods (LPA...) can
     resolve group identity through a stable, versioned contract instead of reaching into LocationExtra,
     which today is tomorrow may be gone i.e. whose shape is not promised to stay fixed. Returns the
     real (group, distance) pairs even when m_group itself is a virtual handle for the multi-group case, 
     so callers never have to know about the virtualization.
    */
    public static List<Tuple<string, float>>? GetLocationGroups(ZoneSystem.ZoneLocation location, bool maxGroup)
    {
        return LocationExtra.GetGroups(location, maxGroup);
    }
}