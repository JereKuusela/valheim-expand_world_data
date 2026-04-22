using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;
using Data;
using System.Diagnostics;
using System;

namespace ExpandWorldData;

public class LocationSpawning
{
  public static ZoneSystem.ZoneLocation? CurrentLocation = null;
  public static DataEntry? DataOverride(DataEntry? pkg, string prefab)
  {
    return LocationExtra.MergeData(CurrentLocation, pkg, prefab);
  }
  public static DataEntry? DungeonDataOverride(string prefab)
  {
    return LocationExtra.GetData(CurrentLocation, prefab, true);
  }
  public static string PrefabOverride(string prefab)
  {
    return LocationExtra.GetPrefabOverride(CurrentLocation, prefab);
  }
  public static string DungeonPrefabOverride(string prefab)
  {
    return LocationExtra.GetPrefabOverride(CurrentLocation, prefab, true);
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
    if (!LocationExtra.TryGet(location, out var extra) || extra?.Objects == null) return;
    var objects = extra.Objects;
    //ExpandWorldData.Log.Debug($"Spawning {objects.Count} custom objects in {location.m_prefab.Name}");
    foreach (var obj in objects)
    {
      if (obj.Chance < 1f && UnityEngine.Random.value > obj.Chance) continue;
      Spawn.BPO(obj, pos, rot, scale, seed, DataOverride, PrefabOverride, spawnedGhostObjects);
    }
  }


}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocationProxy))]
public class LocationZDO
{
  static void Prefix(ZoneSystem __instance, ZoneSystem.ZoneLocation location, Vector3 pos, Quaternion rotation)
  {
    if (!LocationExtra.TryGet(location, out var extra)) return;
    var key = extra.ZDOData;
    if (string.IsNullOrEmpty(key)) return;
    var data = DataHelper.Get(key!, location.m_prefab.Name);
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
      LocationSpawning.CurrentLocation = location;
      if (LocationExtra.TryGetData(location, out var data))
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
    if (LocationExtra.TryGetData(location, out var data))
      WearNTear.m_randomInitialDamage = data.randomDamage == "true" || data.randomDamage == "all";
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var instantiator = AccessTools.FirstMethod(typeof(UnityEngine.Object), info => info.Name == nameof(UnityEngine.Object.Instantiate) && info.IsGenericMethodDefinition &&
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
    if (LocationExtra.TryGetData(location, out var data))
    {
      WearNTear.m_randomInitialDamage = data.randomDamage == "true" || data.randomDamage == "all";
      // Remove the applied offset.
      var surface = pos with { y = pos.y - (data.offset ?? data.groundOffset) };
      HandleTerrain(surface, location.m_exteriorRadius, isBluePrint, data);
    }
    UnityEngine.Random.InitState(seed);
    if (mode == ZoneSystem.SpawnMode.Ghost)
      ZNetView.StartGhostInit();
    var scale = LocationExtra.GetScale(location);
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
    LocationExtra.RunCommand(location, pos, rot);
    LocationSpawning.CurrentLocation = null;
  }

  static void HandleTerrain(Vector3 pos, float radius, bool isBlueprint, LocationYaml data)
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

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocationsTimeSliced), typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage))]
[HarmonyPatch(MethodType.Enumerator)]
public class ScaleLocationHeightRequirement
{
  static float ScaleHeight(float height, Heightmap.Biome biome)
  {
    if (!Configuration.ScaleLocationAltitudeRequirement) return height;
    if (!BiomeManager.TryGetData(biome, out var data))
      return height;

    height *= data.altitudeMultiplier;
    height += data.altitudeDelta;
    if (height < 0f)
      height *= data.waterDepthMultiplier;
    return height;
  }

  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions) =>
      new CodeMatcher(instructions)
        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ZoneSystem.ZoneLocation), nameof(ZoneSystem.ZoneLocation.m_minAltitude))))
        .Advance(1)
        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 9))
        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(ScaleHeight).operand))
        .MatchForward(true, new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ZoneSystem.ZoneLocation), nameof(ZoneSystem.ZoneLocation.m_maxAltitude))))
        .Advance(1)
        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 9))
        .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(ScaleHeight).operand))
        .InstructionEnumeration();

}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones))]
public class CreateLocalZones
{
  public static bool LocationsPregenerated = false;
  static bool Postfix(bool result, ZoneSystem __instance)
  {
    // If vanilla zone generated, wait until next attempt.
    if (result) return result;
    if (LocationsPregenerated) return result;

    foreach (var kvp in __instance.m_locationInstances)
    {
      var loc = kvp.Value.m_location;
      if (loc == null) continue;
      if (!LocationExtra.TryGetData(loc, out var data)) continue;
      if (!data.pregenerate) continue;
      // Vanilla returns true if poke is successful (doesn't fully make sense but it is what it is).
      if (__instance.PokeLocalZone(kvp.Key))
        return true;
      // Poke can return false when generation is not done yet, so have to manually check this.
      if (!__instance.IsZoneGenerated(kvp.Key))
        return false;
    }

    LocationsPregenerated = true;
    return result;
  }
}


[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.HaveLocationInRange))]
public class HaveLocationInRange
{
  static bool Prefix(ref bool __result, ZoneSystem __instance, string prefabName, Vector3 p, float radius, bool maxGroup)
  {
    __result = InRange(__instance, prefabName, p, radius, maxGroup);
    return false;
  }

  private static bool InRange(ZoneSystem zs, string prefabName, Vector3 p, float radius, bool maxGroup)
  {
    var sourceGroups = LocationExtra.GetGroups(LocationSpawning.CurrentLocation, maxGroup);

    foreach (var locationInstance in zs.m_locationInstances.Values)
    {
      var loc = locationInstance.m_location;
      var targetGroups = LocationExtra.GetGroups(loc, maxGroup);
      // Early exit to avoid pointless distance calculation (most locations don't have groups).
      if (loc.m_prefab.Name == prefabName && targetGroups == null) continue;

      var distance = Vector3.Distance(locationInstance.m_position, p);

      // Same prefab check uses the default radius, as there is no group.
      if (loc.m_prefab.Name == prefabName && distance < radius)
        return true;

      if (sourceGroups == null || targetGroups == null) continue;

      foreach (var source in sourceGroups)
      {
        if (distance >= source.Item2) continue;
        if (targetGroups.Any(target => target.Item1 == source.Item1))
          return true;
      }
    }
    return false;
  }
}
