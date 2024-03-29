using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData.Dungeon;

[HarmonyPatch(typeof(DungeonGenerator))]
public class Spawner
{



  ///<summary>Implements object data and swapping from location data.</summary>
  static GameObject CustomObject(GameObject prefab, Vector3 pos, Quaternion rot)
  {
    // Some mods cause client side dungeon reloading. In this case, no data is available.
    // Revert to the default behaviour as a fail safe.
    if (Helper.IsClient()) return Object.Instantiate(prefab, pos, rot);
    BlueprintObject bpo = new(Utils.GetPrefabName(prefab), pos, rot, prefab.transform.localScale, null, 1f);
    var obj = Spawn.BPO(bpo, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
    return obj ?? LocationSpawning.DummySpawn;
  }

  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
  {
    var instantiator = typeof(Object).GetMethods().First(m => m.Name == nameof(Object.Instantiate) && m.IsGenericMethodDefinition && m.GetParameters().Skip(1).Select(p => p.ParameterType).SequenceEqual(new[] { typeof(Vector3), typeof(Quaternion) })).MakeGenericMethod(typeof(GameObject));
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Call, instantiator))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(CustomObject).operand)
      .InstructionEnumeration();
  }

  // Room objects in the dungeon.
  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(DungeonDB.RoomData), typeof(Vector3), typeof(Quaternion), typeof(RoomConnection), typeof(ZoneSystem.SpawnMode)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> DungeonSpawnObjects(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);

  // Doors in the dungeon.
  [HarmonyPatch(nameof(DungeonGenerator.PlaceDoors)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> DungeonSpawnDoors(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);


  [HarmonyPatch(nameof(DungeonGenerator.Generate), typeof(ZoneSystem.SpawnMode)), HarmonyPrefix]
  static void Generate(DungeonGenerator __instance, ZoneSystem.SpawnMode mode)
  {
    if (LocationSpawning.CurrentLocation == "" || mode == ZoneSystem.SpawnMode.Client || Helper.IsClient()) return;
    var dungeonName = Utils.GetPrefabName(__instance.gameObject);
    if (LocationLoading.LocationData.TryGetValue(LocationSpawning.CurrentLocation, out var data) && data.dungeon != "")
      dungeonName = data.dungeon;
    Override(__instance, dungeonName);
    DungeonObjects.CurrentDungeon = dungeonName;
  }

  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(DungeonDB.RoomData), typeof(Vector3), typeof(Quaternion), typeof(RoomConnection), typeof(ZoneSystem.SpawnMode)), HarmonyPrefix]
  static bool ReplaceRoom(ref DungeonDB.RoomData room, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode)
  {
    if (!Configuration.DataRooms || Helper.IsClient()) return true;
    // Clients already have proper rooms.
    if (mode == ZoneSystem.SpawnMode.Client) return true;
    var parameters = room.m_room;
    DungeonObjects.CurrentRoom = parameters.name;
    var baseName = Parse.Name(parameters.name);
    // Combine the base room prefab and the room parameters.
    if (RoomSpawning.Prefabs.TryGetValue(baseName, out var roomData))
    {
      // The proxy shouldn't be modified, or entries will get reference to the same room.
      room = new()
      {
        m_room = RoomSpawning.OverrideParameters(parameters, roomData.m_room),
        m_netViews = roomData.m_netViews,
        m_randomSpawns = roomData.m_randomSpawns
      };
      return true;
    }
    if (BlueprintManager.TryGet(parameters.name, out var bp))
    {
      Spawn.Blueprint(bp, pos, rot, Vector3.one, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
    }
    return true;
  }


  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(DungeonDB.RoomData), typeof(Vector3), typeof(Quaternion), typeof(RoomConnection), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void PlaceRoomCustomObjects(DungeonDB.RoomData room, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode)
  {
    if (!Configuration.DataRooms || mode == ZoneSystem.SpawnMode.Client || Helper.IsClient()) return;
    if (!DungeonObjects.Objects.TryGetValue(room.m_room.name, out var objects)) return;
    int seed = (int)pos.x * 4271 + (int)pos.y * 9187 + (int)pos.z * 2134;
    Random.State state = Random.state;
    Random.InitState(seed);
    foreach (var obj in objects)
    {
      if (obj.Chance < 1f && Random.value > obj.Chance) continue;
      Spawn.BPO(obj, pos, rot, Vector3.one, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
    }
    Random.state = state;
    DungeonObjects.CurrentRoom = "";
  }


  [HarmonyPatch(nameof(DungeonGenerator.Generate), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void GenerateEnd()
  {
    DungeonObjects.CurrentDungeon = "";
  }



  // The dungeon prefab is only used for generating, so the properties can be just overwritten.
  public static void Override(DungeonGenerator dg, string name)
  {
    if (!DungeonObjects.Generators.TryGetValue(name, out var data)) return;
    //ExpandWorldData.Log.Debug($"Overriding with dungeon {name}.");
    dg.name = name;
    dg.m_algorithm = data.m_algorithm;
    dg.m_zoneSize = data.m_zoneSize;
    dg.m_alternativeFunctionality = data.m_alternativeFunctionality;
    dg.m_campRadiusMax = data.m_campRadiusMax;
    dg.m_campRadiusMin = data.m_campRadiusMin;
    dg.m_doorChance = data.m_doorChance;
    dg.m_doorTypes = data.m_doorTypes;
    dg.m_maxRooms = data.m_maxRooms;
    dg.m_minRooms = data.m_minRooms;
    dg.m_maxTilt = data.m_maxTilt;
    dg.m_minAltitude = data.m_minAltitude;
    dg.m_minRequiredRooms = data.m_minRequiredRooms;
    dg.m_requiredRooms = data.m_requiredRooms;
    dg.m_themes = DataManager.ToEnum<Room.Theme>(data.m_themes);
    dg.m_gridSize = data.m_gridSize;
    dg.m_tileWidth = data.m_tileWidth;
    dg.m_spawnChance = data.m_spawnChance;
    dg.m_perimeterSections = data.m_perimeterSections;
    dg.m_perimeterBuffer = data.m_perimeterBuffer;
    dg.m_useCustomInteriorTransform = data.m_useCustomInteriorTransform;
  }
  [HarmonyPatch(nameof(DungeonGenerator.SetupAvailableRooms)), HarmonyPostfix]
  public static void SetupAvailableRooms(DungeonGenerator __instance)
  {
    if (Helper.IsClient()) return;
    var name = Utils.GetPrefabName(__instance.gameObject);
    if (!DungeonObjects.Generators.TryGetValue(name, out var gen)) return;
    if (gen.m_excludedRooms.Count == 0) return;
    DungeonGenerator.m_availableRooms = DungeonGenerator.m_availableRooms.Where(room => !gen.m_excludedRooms.Contains(room.m_room.name)).ToList();

  }

}

// Client side tweak to make the dungeon environment box extend to the whole dungeon.
[HarmonyPatch]
public class EnvironmentBox
{

  // Not fully sure if the generator or location loads first.
  public static Dictionary<Vector2i, Vector3> Cache = [];


  private static void TryScale(Location loc)
  {
    var zone = ZoneSystem.instance.GetZone(loc.transform.position);
    if (!Cache.TryGetValue(zone, out var size)) return;
    // Only interior locations can have the environment box.
    if (!loc.m_hasInterior) return;
    var envZone = loc.GetComponentInChildren<EnvZone>();
    if (!envZone) return;
    // Don't shrink from the default so that people can build there more easily.
    // Otherwise for small dungeons the box would be very small.
    var origSize = envZone.transform.localScale;
    size.x = Mathf.Max(size.x, origSize.x);
    size.y = Mathf.Max(size.y, origSize.y);
    size.z = Mathf.Max(size.z, origSize.z);
    // ExpandWorldData.Log.Debug($"Scaling environment box for {loc.name} from {origSize} to {size}.");
    envZone.transform.localScale = size;
  }

  [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Load)), HarmonyPostfix]
  static void ScaleEnvironmentBox1(DungeonGenerator __instance)
  {
    var pos = __instance.transform.position;
    var zone = ZoneSystem.instance.GetZone(pos);
    var center = ZoneSystem.instance.GetZonePos(zone) with { y = pos.y };
    var colliders = __instance.GetComponentsInChildren<Room>().SelectMany(room => room.GetComponentsInChildren<BoxCollider>()).ToList();
    Bounds bounds = new()
    {
      center = center,
    };
    foreach (var c in colliders)
      bounds.Encapsulate(c.bounds);
    // Bounds doesn't keep the center point, so manually calculate the biggest size.
    var offset = bounds.center - center;
    var extents = Vector3.zero;
    // Vanilla mountain caves seemed to overflow a bit, so make the box smaller to reduce chance of dungeons clipping.
    var tweak = -1f;
    extents.x = Mathf.Max(Mathf.Abs(offset.x + bounds.extents.x) + tweak, Mathf.Abs(offset.x - bounds.extents.x) + tweak);
    extents.y = Mathf.Max(Mathf.Abs(offset.y + bounds.extents.y) + tweak, Mathf.Abs(offset.y - bounds.extents.y) + tweak);
    extents.z = Mathf.Max(Mathf.Abs(offset.z + bounds.extents.z) + tweak, Mathf.Abs(offset.z - bounds.extents.z) + tweak);
    // ExpandWorldData.Log.Debug($"Bounds for {__instance.name} are {bounds.center} {bounds.extents} {center}.");
    Cache[zone] = 2 * extents;
    var locsInZone = Location.m_allLocations.Where(loc => ZoneSystem.instance.GetZone(loc.transform.position) == zone).ToArray();
    foreach (var loc in locsInZone)
      TryScale(loc);
  }

  [HarmonyPatch(typeof(Location), nameof(Location.Awake)), HarmonyPostfix]
  static void ScaleEnvironmentBox2(Location __instance)
  {
    TryScale(__instance);
  }
}