

using System;
using System.Collections.Generic;
using Data;
using Service;

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
    var locationData = LocationSpawning.DungeonDataOverride(prefab);
    var dungeonData = DataDungeonOverride(prefab);
    var roomData = DataRoomOverride(prefab);
    return DataHelper.Merge(locationData, dungeonData, roomData, pgk);
  }
  private static DataEntry? DataDungeonOverride(string prefab)
  {
    if (!Generators.TryGetValue(CurrentDungeon, out var gen)) return null;
    var allData = gen.m_objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = gen.m_objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return DataHelper.Merge(allData, prefabData);
  }
  public static DataEntry? DataRoomOverride(string prefab)
  {
    if (!ObjectData.TryGetValue(CurrentRoom!, out var objectData)) return null;
    var allData = objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return DataHelper.Merge(allData, prefabData);
  }

}