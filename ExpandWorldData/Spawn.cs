using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Data;

using DataOverride = System.Func<Data.DataEntry?, string, Data.DataEntry?>;
using Service;

namespace ExpandWorldData;

// Most of the custom object spawning should be done here because there are many pitfalls:
// 1. If custom data is used, ghost init is not used and must be done manually.
// 2. On ghost spawning mode, created objects must be either stored or instantly destroyed.
public class Spawn
{
  public static bool IgnoreHealth = false;
  public static void Blueprint(string name, Vector3 pos, Quaternion rot, Vector3 scale, int seed, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
  {
    if (BlueprintManager.TryGet(name, out var bp))
      Blueprint(bp, pos, rot, scale, seed, dataOverride, prefabOverride, spawned);
  }
  public static void Blueprint(Blueprint bp, Vector3 pos, Quaternion rot, Vector3 scale, int seed, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
  {
    foreach (var obj in bp.Objects)
    {
      if (obj.Chance < 1f && UnityEngine.Random.value > obj.Chance) continue;
      BPO(obj, pos, rot, scale, seed, dataOverride, prefabOverride, spawned);
    }
  }
  private static void SetData(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, int seed, DataEntry? data = null)
  {
    // No override needed.
    if (data == null && scale == null) return;
    var zdo = DataHelper.Init(prefab, position, rotation, scale, data);
    // Users very easily might have creator on their blueprints or copied data.
    // This causes enemies to attack them because they are considered player built.
    // So far no reason to keep this data.
    zdo?.RemoveLong(ZDOVars.s_creator);
    // For random damage, health is not needed.
    if (IgnoreHealth)
      zdo?.RemoveFloat(ZDOVars.s_health);
    // Blueprints can include locations, these would have fixed seed which messes up the random system.
    if (prefab.name == "LocationProxy")
      zdo?.Set(ZDOVars.s_seed, seed);
  }

  // Spawning a object should support following scenarions:
  // 1. Adding an object with random data.
  // 2. Adding an object with random prefab.
  // 3. Adding an object with a specific data, regardless of other configuration.
  // 4. Adding an object with a specific prefab, regardless of other configuration.
  //
  // 1. can be achieved by applying objectData to every object.
  // 2. can be achieved by applying objectSwap to every object.
  // 3. can be achieved by skipping objectData if the object has already custom data.
  // 4. can be achieved by using a dummy object and then objectSwap to replace it.

  // This should be only called for custom objects and blueprints so not returning anything.
  public static void BPO(BlueprintObject obj, Vector3 pos, Quaternion rot, Vector3 scale, int seed, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
  {
    var go = SharedBPO(obj, pos, rot, scale, seed, dataOverride, prefabOverride, spawned);
    if (go != null && go.TryGetComponent<DungeonGenerator>(out var dg))
    {
      // m_originalPosition is not set because not sure if it's needed for custom objects.
      dg.Generate(ZNetView.m_ghostInit ? ZoneSystem.SpawnMode.Ghost : ZoneSystem.SpawnMode.Full);
    }
  }
  // Needed for EW Spawns.
  public static void BPO(BlueprintObject obj, Vector3 pos, Quaternion rot, Vector3 scale, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
   => BPO(obj, pos, rot, scale, 0, dataOverride, prefabOverride, spawned);

  // This is called for vanilla objects so it must be returned to the original code.
  public static GameObject? BPO(BlueprintObject obj, int seed, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
  {
    // Dungeon generate not called here because it's called in the original location code.
    return SharedBPO(obj, Vector3.zero, Quaternion.identity, Vector3.one, seed, dataOverride, prefabOverride, spawned);
  }
  private static GameObject? SharedBPO(BlueprintObject obj, Vector3 pos, Quaternion rot, Vector3 scale, int seed, DataOverride dataOverride, Func<string, string> prefabOverride, List<GameObject>? spawned)
  {
    pos += rot * obj.Pos;
    if (obj.SnapToGround)
    {
      if (ZoneSystem.instance.GetGroundHeight(pos, out var height))
        pos.y = height;
      else
        pos.y = WorldGenerator.instance.GetHeight(pos.x, pos.z);
    }
    rot *= obj.Rot;
    var sc = scale;
    if (obj.Scale.HasValue)
    {
      sc.x *= obj.Scale.Value.x;
      sc.y *= obj.Scale.Value.y;
      sc.z *= obj.Scale.Value.z;
    }
    var prefabName = prefabOverride(obj.Prefab);
    // Empty is valid for removing objects.
    if (prefabName == "")
      return null;
    var prefab = ZNetScene.instance.GetPrefab(prefabName);
    if (!prefab)
    {
      if (BlueprintManager.TryGet(prefabName, out var bp))
      {
        Blueprint(bp, pos, rot, sc, seed, dataOverride, prefabOverride, spawned);
        return null;
      }
      Log.Warning($"Blueprint / object prefab {prefabName} not found!");
      return null;
    }
    var data = dataOverride(obj.Data, prefabName);
    SetData(prefab, pos, rot, sc, seed, data);

    //ExpandWorldData.Log.Debug($"Spawning {prefabName} at {Helper.Print(pos)} {source}");
    var go = UnityEngine.Object.Instantiate(prefab, pos, rot);
    DataManager.CleanGhostInit(go);
    if (ZNetView.m_ghostInit)
    {
      if (spawned != null)
        spawned.Add(go);
      // Vanilla code also calls Destroy in some cases but this doesn't matter.
      else
        UnityEngine.Object.Destroy(go);
    }
    return go;
  }

  private static List<Tuple<float, string>> ParseSwapItems(IEnumerable<string> items, float weight) => items.Select(s => Parse.Split(s, false, ':')).Select(s => Tuple.Create(Parse.Float(s, 1, 1f) * weight, s[0])).ToList();


  public static Dictionary<string, List<Tuple<float, string>>> LoadSwaps(string[] objectSwap)
  {
    Dictionary<string, List<Tuple<float, string>>> swaps = [];
    // Empty items are kept to support spawning nothing.
    var list = objectSwap.Select(s => DataManager.ToList(s, false)).Where(l => l.Count > 0).ToList();
    // Complicated logic to support:
    // 1. Multiple rows for the same object.
    // 2. Multiple swaps in the same row.
    foreach (var row in list)
    {
      var s = Parse.Split(row[0], true, ':');
      var prefabs = DataHelper.ResolvePrefabs(s[0]);
      var weight = Parse.Float(s, 1, 1f);
      var swapItems = ParseSwapItems(row.Skip(1), weight);
      foreach (var prefab in prefabs)
      {
        if (!swaps.ContainsKey(prefab))
          swaps[prefab] = [];
        swaps[prefab].AddRange(swapItems);
      }
    }
    foreach (var kvp in swaps)
    {
      var total = kvp.Value.Sum(t => t.Item1);
      for (var i = 0; i < kvp.Value.Count; ++i)
        kvp.Value[i] = Tuple.Create(kvp.Value[i].Item1 / total, kvp.Value[i].Item2);
      foreach (var swap in kvp.Value)
      {
        // Empty string is supported for removing objects.
        if (swap.Item2 != "")
          BlueprintManager.Load(swap.Item2);
      }

    }
    return swaps;
  }

  private static List<Tuple<float, DataEntry?>> ParseDataItems(IEnumerable<string> items, float weight, string fileName) => items.Select(s => Parse.Split(s, false, ':')).Select(s => Tuple.Create(Parse.Float(s, 1, 1f) * weight, DataHelper.Get(s[0], fileName))).ToList();
  public static Dictionary<string, List<Tuple<float, DataEntry?>>> LoadData(string[] objectData, string fileName)
  {
    Dictionary<string, List<Tuple<float, DataEntry?>>> datas = [];
    // Empty items are kept to support spawning nothing.
    var list = objectData.Select(s => DataManager.ToList(s, false)).Where(l => l.Count > 0).ToList();
    // Complicated logic to support:
    // 1. Multiple rows for the same object.
    // 2. Multiple data in the same row.
    // 3. Value groups.
    var scene = ZNetScene.instance;
    foreach (var row in list)
    {
      var s = Parse.Split(row[0], true, ':');
      var prefabs = DataHelper.ResolvePrefabs(s[0]);
      var name = s[0];
      var weight = Parse.Float(s, 1, 1f);
      var dataItems = ParseDataItems(row.Skip(1), weight, fileName);
      foreach (var prefab in prefabs)
      {
        if (!datas.ContainsKey(prefab))
          datas[prefab] = [];
        datas[prefab].AddRange(dataItems);
      }
    }
    foreach (var kvp in datas)
    {
      var total = kvp.Value.Sum(t => t.Item1);
      for (var i = 0; i < kvp.Value.Count; ++i)
        kvp.Value[i] = Tuple.Create(kvp.Value[i].Item1 / total, kvp.Value[i].Item2);
    }
    return datas;
  }
  public static string RandomizeSwap(List<Tuple<float, string>> swaps)
  {
    if (swaps.Count == 0)
      return "";
    if (swaps.Count == 1)
      return swaps[0].Item2;
    var rng = UnityEngine.Random.value;
    //Log.Warning($"RandomizeSwap: Roll {Helper.Print(rng)} for {string.Join(", ", swaps.Select(s => s.Item2 + ":" + Helper.Print(s.Item1)))}");
    foreach (var swap in swaps)
    {
      rng -= swap.Item1;
      if (rng <= 0f) return swap.Item2;
    }
    return swaps[swaps.Count - 1].Item2;
  }
  public static DataEntry? RandomizeData(List<Tuple<float, DataEntry?>> swaps)
  {
    if (swaps.Count == 0)
      return null;
    if (swaps.Count == 1)
      return swaps[0].Item2;
    var rng = UnityEngine.Random.value;
    //Log.Warning($"RandomizeData: Roll {Helper.Print(rng)} for weigths {string.Join(", ", swaps.Select(s => Helper.Print(s.Item1)))}");
    foreach (var swap in swaps)
    {
      rng -= swap.Item1;
      if (rng <= 0f) return swap.Item2;
    }
    return swaps[swaps.Count - 1].Item2;
  }
}