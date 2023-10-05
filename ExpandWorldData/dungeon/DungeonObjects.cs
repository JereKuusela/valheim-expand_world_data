

using System;
using System.Collections.Generic;
using Service;

namespace ExpandWorldData.Dungeon;

public static class DungeonObjects
{

  public static Dictionary<string, FakeDungeonGenerator> Generators = [];
  public static string CurrentDungeon = "";
  public static string CurrentRoom = "";


  public static Dictionary<string, Dictionary<string, List<Tuple<float, ZDOData?>>>> ObjectData = [];

  public static Dictionary<string, List<BlueprintObject>> Objects = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, string>>>> ObjectSwaps = [];

  public static string PrefabOverride(string prefab)
  {
    prefab = LocationSpawning.PrefabOverride(prefab);
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
    if (!ObjectSwaps.TryGetValue(CurrentRoom, out var objectSwaps)) return prefab;
    if (!objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }

  public static ZDOData? DataOverride(ZDOData? pgk, string prefab)
  {
    EWD.Log.LogWarning($"DataOverride {prefab}");
    var locationData = LocationSpawning.DataOverride(prefab);
    var dungeonData = DataDungeonOverride(prefab);
    var roomData = DataRoomOverride(prefab);
    return ZDOData.Merge(locationData, dungeonData, roomData, pgk);
  }
  private static ZDOData? DataDungeonOverride(string prefab)
  {
    if (!Generators.TryGetValue(CurrentDungeon, out var gen)) return null;
    var allData = gen.m_objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = gen.m_objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return ZDOData.Merge(allData, prefabData);
  }
  public static ZDOData? DataRoomOverride(string prefab)
  {
    if (!ObjectData.TryGetValue(CurrentRoom, out var objectData)) return null;
    var allData = objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return ZDOData.Merge(allData, prefabData);
  }

}