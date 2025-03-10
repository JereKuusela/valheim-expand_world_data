using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;
using Data;

namespace ExpandWorldData;

public class LocationSpawning
{
  public static string CurrentLocation = "";
  public static DataEntry? DataOverride(DataEntry? pkg, string prefab)
  {
    if (!LocationLoading.LocationObjectData.TryGetValue(CurrentLocation, out var objectData)) return pkg;
    var allData = objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return DataHelper.Merge(allData, prefabData, pkg);
  }
  public static DataEntry? DungeonDataOverride(string prefab)
  {
    if (!LocationLoading.DungeonObjectData.TryGetValue(CurrentLocation, out var objectData)) return null;
    var allData = objectData.TryGetValue("all", out var data1) ? Spawn.RandomizeData(data1) : null;
    var prefabData = objectData.TryGetValue(prefab, out var data2) ? Spawn.RandomizeData(data2) : null;
    return DataHelper.Merge(allData, prefabData);
  }
  public static string PrefabOverride(string prefab)
  {
    if (!LocationLoading.LocationObjectSwaps.TryGetValue(CurrentLocation, out var objectSwaps)) return prefab;
    if (!objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }
  public static string DungeonPrefabOverride(string prefab)
  {
    if (!LocationLoading.DungeonObjectSwaps.TryGetValue(CurrentLocation, out var objectSwaps)) return prefab;
    if (!objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }
  static readonly string DummyObj = "vfx_auto_pickup";
  public static GameObject DummySpawn => UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab(DummyObj), Vector3.zero, Quaternion.identity);
  public static GameObject Object(GameObject prefab, Vector3 pos, Quaternion rot, int seed, List<GameObject> spawnedGhostObjects)
  {
    BlueprintObject bpo = new(Utils.GetPrefabName(prefab), pos, rot, prefab.transform.localScale, null, 1f);
    var obj = Spawn.BPO(bpo, seed, DataOverride, PrefabOverride, spawnedGhostObjects);
    return obj ?? DummySpawn;
  }


  public static void CustomObjects(ZoneSystem.ZoneLocation location, Vector3 pos, Quaternion rot, Vector3 scale, int seed, List<GameObject> spawnedGhostObjects)
  {
    if (!LocationLoading.Objects.TryGetValue(location.m_prefab.Name, out var objects)) return;
    //ExpandWorldData.Log.Debug($"Spawning {objects.Count} custom objects in {location.m_prefab.Name}");
    foreach (var obj in objects)
    {
      if (obj.Chance < 1f && Random.value > obj.Chance) continue;
      Spawn.BPO(obj, pos, rot, scale, seed, DataOverride, PrefabOverride, spawnedGhostObjects);
    }
  }


}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocationProxy))]
public class LocationZDO
{
  static void Prefix(ZoneSystem __instance, ZoneSystem.ZoneLocation location, Vector3 pos, Quaternion rotation)
  {
    if (!LocationLoading.ZDOData.TryGetValue(location.m_prefab.Name, out var key)) return;
    var data = DataHelper.Get(key, location.m_prefab.Name);
    if (data != null) DataHelper.Init(__instance.m_locationProxyPrefab, pos, rotation, null, data);
  }
}
[HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SetLocation))]
public class FixGhostInit
{
  static void Prefix(LocationProxy __instance, ref string location, ref bool spawnNow)
  {
    location = Parse.Name(location);
    if (ZNetView.m_ghostInit)
    {
      spawnNow = false;
      DataManager.CleanGhostInit(__instance.m_nview);
    }
  }
}


[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SpawnLocation))]
public class LocationObjectDataAndSwap
{
  static bool Prefix(ZoneSystem.ZoneLocation location, ZoneSystem.SpawnMode mode, ref Vector3 pos, ref int seed)
  {
    if (mode != ZoneSystem.SpawnMode.Client)
    {
      LocationSpawning.CurrentLocation = location.m_prefab.Name;
      if (LocationLoading.LocationData.TryGetValue(location.m_prefab.Name, out var data))
      {
        Spawn.IgnoreHealth = data.randomDamage == "all";
        pos.y += data.offset ?? data.groundOffset;
        if (Configuration.RandomLocations || data.randomSeed) seed = System.DateTime.Now.Ticks.GetHashCode();
      }
    }
    // Blueprints won't have any znetviews to spawn or other logic to handle.
    return location.m_prefab.IsValid;
  }
  static void Customize(ZoneSystem.ZoneLocation location)
  {
    if (LocationLoading.LocationData.TryGetValue(location.m_prefab.Name, out var data))
      WearNTear.m_randomInitialDamage = data.randomDamage == "true" || data.randomDamage == "all";
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var instantiator = AccessTools.FirstMethod(typeof(Object), info => info.Name == nameof(Object.Instantiate) && info.IsGenericMethodDefinition &&
            info.GetParameters().Length == 3 &&
            info.GetParameters()[1].ParameterType == typeof(Vector3) &&
            info.GetParameters()[2].ParameterType == typeof(Quaternion))
      .MakeGenericMethod(typeof(GameObject));
    return new CodeMatcher(instructions)
      .MatchForward(true, new CodeMatch(OpCodes.Stsfld, AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_randomInitialDamage))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(Customize).operand))
      .MatchForward(false, new CodeMatch(OpCodes.Call, instantiator))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_2))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_S, 6))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(LocationSpawning.Object).operand)
      .InstructionEnumeration();
  }


  static void Postfix(ZoneSystem.ZoneLocation location, int seed, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode, List<GameObject> spawnedGhostObjects)
  {
    // Previously client mode cleared CurrentLocation which caused issued on single player.
    // If the player teleports to the location, location placement would also run the client spawning.
    if (mode == ZoneSystem.SpawnMode.Client) return;

    var isBluePrint = BlueprintManager.Has(location.m_prefab.Name);
    if (LocationLoading.LocationData.TryGetValue(location.m_prefab.Name, out var data))
    {
      WearNTear.m_randomInitialDamage = data.randomDamage == "true" || data.randomDamage == "all";
      // Remove the applied offset.
      var surface = pos with { y = pos.y - (data.offset ?? data.groundOffset) };
      HandleTerrain(surface, location.m_exteriorRadius, isBluePrint, data);
    }
    Random.InitState(seed);
    if (mode == ZoneSystem.SpawnMode.Ghost)
      ZNetView.StartGhostInit();
    var scale = Vector3.one;
    if (LocationLoading.Scales.TryGetValue(location.m_prefab.Name, out var s)) scale = Helper.RandomValue(s);
    if (isBluePrint && BlueprintManager.TryGet(location.m_prefab.Name, out var bp))
    {
      Spawn.Blueprint(bp, pos, rot, scale, seed, LocationSpawning.DataOverride, LocationSpawning.PrefabOverride, spawnedGhostObjects);
    }
    LocationSpawning.CustomObjects(location, pos, rot, scale, seed, spawnedGhostObjects);

    WearNTear.m_randomInitialDamage = false;
    SnapToGround.SnappAll();
    if (mode == ZoneSystem.SpawnMode.Ghost)
      ZNetView.FinishGhostInit();
    Spawn.IgnoreHealth = false;
    if (LocationLoading.Commands.TryGetValue(location.m_prefab.Name, out var commands))
      CommandManager.Run(commands, pos, rot.eulerAngles);
    LocationSpawning.CurrentLocation = "";
  }

  static void HandleTerrain(Vector3 pos, float radius, bool isBlueprint, LocationData data)
  {
    var level = false;
    if (data.levelArea == "") level = isBlueprint;
    else if (data.levelArea == "false") level = false;
    else level = true;
    if (!level && data.paint == "") return;

    Terrain.ChangeTerrain(pos, (hm, terrain) =>
    {
      if (level)
      {
        var levelRadius = data.levelRadius;
        var levelBorder = data.levelBorder;
        if (levelRadius == 0f && levelBorder == 0f)
        {
          var multiplier = Parse.Float(data.levelArea, 0.5f);
          levelRadius = multiplier * radius;
          levelBorder = (1 - multiplier) * radius;
        }
        Terrain.Level(hm, terrain, pos, levelRadius, levelBorder);
      }
      if (data.paint != "")
      {
        var paintRadius = data.paintRadius ?? radius;
        var paintBorder = data.paintBorder ?? 5f;
        Terrain.Paint(hm, terrain, pos, data.paint, paintRadius, paintBorder);
      }
    });
  }
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.PokeCanSpawnLocation))]
  public class PokeCanSpawnLocation
  {
    static bool Prefix(ZoneSystem.ZoneLocation location, ref bool __result)
    {
      if (BlueprintManager.Has(location.m_prefab.Name))
      {
        __result = true;
        return false;
      }
      return true;
    }
  }
}
