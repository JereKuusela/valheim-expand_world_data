

using System;
using System.Collections.Generic;
using Data;

namespace ExpandWorldData.Dungeon;

public static class DungeonObjects
{

  public static Dictionary<string, FakeDungeonGenerator> Generators = [];
  public static string CurrentDungeon = "";
  public static DungeonDB.RoomData? CurrentRoom;


  public static Dictionary<DungeonDB.RoomData, Dictionary<string, List<Tuple<float, DataEntry?>>>> ObjectData = [];

  public static Dictionary<DungeonDB.RoomData, List<BlueprintObject>> Objects = [];
  public static Dictionary<DungeonDB.RoomData, Dictionary<string, List<Tuple<float, string>>>> ObjectSwaps = [];

  public static string PrefabOverride(string prefab)
  {
    prefab = LocationSpawning.DungeonPrefabOverride(prefab);
    prefab = PrefabDungeonOverride(prefab);
    prefab = PrefabRoomOverride(prefab);
    return prefab;
  }
  private static string PrefabDungeonOverride(string prefab)
  {
    if (!Generators.TryGetValue(CurrentDungeon, out var gen)) return prefab;
    if (!gen.m_objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }
  public static string PrefabRoomOverride(string prefab)
  {
    if (!ObjectSwaps.TryGetValue(CurrentRoom!, out var objectSwaps)) return prefab;
    if (!objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }

  public static DataEntry? DataOverride(DataEntry? pgk, string prefab)
  {
    DataEntry? result = null;
    result = DataHelper.Merge(result, LocationSpawning.DungeonDataOverride(prefab));
    result = DataHelper.Merge(result, DataDungeonOverride(prefab));
    result = DataHelper.Merge(result, DataRoomOverride(prefab));
    return DataHelper.Merge(result, pgk);
  }

  private static DataEntry? DataDungeonOverride(string prefab)
  {
    if (!Generators.TryGetValue(CurrentDungeon, out var gen)) return null;
    return Spawn.GetData(gen.m_objectData, prefab);
  }
  public static DataEntry? DataRoomOverride(string prefab)
  {
    if (!ObjectData.TryGetValue(CurrentRoom!, out var objectData)) return null;
    return Spawn.GetData(objectData, prefab);
  }

}