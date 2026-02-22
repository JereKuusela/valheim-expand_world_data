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
    if (DungeonObjects.CurrentRoom == null) return Object.Instantiate(prefab, pos, rot);
    BlueprintObject bpo = new(Utils.GetPrefabName(prefab), pos, rot, prefab.transform.localScale, null, 1f);
    var obj = Spawn.BPO(bpo, 0, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
    return obj ?? LocationSpawning.DummySpawn;
  }

  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
  {
    var instantiator = AccessTools.FirstMethod(typeof(Object), info => info.Name == nameof(Object.Instantiate) && info.IsGenericMethodDefinition &&
            info.GetParameters().Length == 3 &&
            info.GetParameters()[1].ParameterType == typeof(Vector3) &&
            info.GetParameters()[2].ParameterType == typeof(Quaternion))
      .MakeGenericMethod(typeof(GameObject));
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Call, instantiator))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(CustomObject).operand)
      .InstructionEnumeration();
  }

  // The code loads connections from the prefab, instead of the room data. This overrides any customization.
  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(RoomConnection), typeof(DungeonDB.RoomData), typeof(ZoneSystem.SpawnMode)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> FixConnections(IEnumerable<CodeInstruction> instructions)
  {
    // Replaces the code that gets room compopnent from prefab with our saved data.
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Stloc_1, null))
      .Advance(-3)
      .SetAndAdvance(OpCodes.Ldarg_2, null)
      .SetAndAdvance(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(DungeonDB.RoomData), nameof(DungeonDB.RoomData.RoomInPrefab)))
      .SetAndAdvance(OpCodes.Nop, null)
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

  [HarmonyPatch(nameof(DungeonGenerator.GetWeightedRoom)), HarmonyPrefix]
  // Prevents index out of bounds error.
  static bool GetWeightedRoomIndexCheck(List<DungeonDB.RoomData> rooms) => rooms.Count > 0;

  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(RoomConnection), typeof(DungeonDB.RoomData), typeof(ZoneSystem.SpawnMode)), HarmonyPrefix]
  // Prevent null reference error.
  static bool PlaceRoomNullCheck(RoomConnection connection, DungeonDB.RoomData roomData)
  {
    if (roomData == null)
    {
      Log.Warning("No room found for connection type " + connection.m_type);
      return false;
    }
    return true;
  }



  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(DungeonDB.RoomData), typeof(Vector3), typeof(Quaternion), typeof(RoomConnection), typeof(ZoneSystem.SpawnMode)), HarmonyPrefix]
  static void SpawnBlueprintRoom(DungeonDB.RoomData roomData, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode)
  {
    // Clients already have proper rooms.
    if (!Configuration.DataRooms || mode == ZoneSystem.SpawnMode.Client || Helper.IsClient()) return;
    DungeonObjects.CurrentRoom = roomData;
    if (!RoomSpawning.Blueprints.TryGetValue(roomData, out var bpName))
      return;

    if (BlueprintManager.TryGet(bpName, out var bp))
      Spawn.Blueprint(bp, pos, rot, Vector3.one, 0, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
  }


  [HarmonyPatch(nameof(DungeonGenerator.PlaceRoom), typeof(DungeonDB.RoomData), typeof(Vector3), typeof(Quaternion), typeof(RoomConnection), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void PlaceRoomCustomObjects(DungeonDB.RoomData roomData, Vector3 pos, Quaternion rot, ZoneSystem.SpawnMode mode)
  {
    if (!Configuration.DataRooms || mode == ZoneSystem.SpawnMode.Client || Helper.IsClient()) return;
    if (DungeonObjects.Objects.TryGetValue(roomData, out var objects))
    {
      int seed = (int)pos.x * 4271 + (int)pos.y * 9187 + (int)pos.z * 2134;
      Random.State state = Random.state;
      Random.InitState(seed);
      foreach (var obj in objects)
      {
        if (obj.Chance < 1f && Random.value > obj.Chance) continue;
        Spawn.BPO(obj, pos, rot, Vector3.one, 0, DungeonObjects.DataOverride, DungeonObjects.PrefabOverride, null);
      }
      Random.state = state;
    }
    DungeonObjects.CurrentRoom = null;
  }


  [HarmonyPatch(nameof(DungeonGenerator.Generate), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void GenerateEnd()
  {
    DungeonObjects.CurrentDungeon = "";
  }
  [HarmonyPatch(nameof(DungeonGenerator.GenerateRooms), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void GenerateRooms()
  {
    Log.Info($"Dungeon generated with {DungeonGenerator.m_placedRooms.Count} rooms.");
  }
  [HarmonyPatch(nameof(DungeonGenerator.GetSeed)), HarmonyPrefix]
  static void GetSeed()
  {
    if (Configuration.RandomLocations)
      DungeonGenerator.m_forceSeed = System.DateTime.Now.Ticks.GetHashCode();
    else if (Configuration.DataLocation && LocationLoading.LocationData.TryGetValue(LocationSpawning.CurrentLocation, out var locData) && locData.randomSeed)
      DungeonGenerator.m_forceSeed = System.DateTime.Now.Ticks.GetHashCode();
    else if (Configuration.DataDungeons && DungeonObjects.Generators.TryGetValue(DungeonObjects.CurrentDungeon, out var genData) && genData.m_randomSeed)
      DungeonGenerator.m_forceSeed = System.DateTime.Now.Ticks.GetHashCode();
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
    DungeonGenerator.m_availableRooms = DungeonGenerator.m_availableRooms.Where(room => !gen.m_excludedRooms.Contains(room.m_prefab.Name)).ToList();
  }

  private static readonly List<RoomConnection> endConnections = [];

  private static void MoveConnection(RoomConnection conn)
  {
    endConnections.Add(conn);
    DungeonGenerator.m_openConnections.Remove(conn);
  }

  [HarmonyPatch(nameof(DungeonGenerator.PlaceOneRoom)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> PlaceOneRoom(IEnumerable<CodeInstruction> instructions)
  {
    // Original code doesn't remove failed connections, so the same connection can be tried multiple times (which is bit wasteful).
    // This moves them to endConnections list to be handled later.
    return new CodeMatcher(instructions)
      .End()
      .Advance(-1)
      .Insert(new CodeInstruction(OpCodes.Ldloc_0), new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(MoveConnection).operand))
      .InstructionEnumeration();
  }

  [HarmonyPatch(nameof(DungeonGenerator.CheckRequiredRooms)), HarmonyPrefix]
  public static bool CheckRequiredRooms(ref bool __result)
  {
    // This is used as early exit if ther are no connections left to try.
    if (DungeonGenerator.m_openConnections.Count == 0)
    {
      __result = true;
      return false;
    }
    return true;
  }

  // This is needed to restore connection for end cap handling.
  [HarmonyPatch(nameof(DungeonGenerator.PlaceRooms)), HarmonyPostfix]
  public static void PlaceRooms()
  {
    foreach (var conn in endConnections)
    {
      DungeonGenerator.m_openConnections.Add(conn);
    }
    endConnections.Clear();
  }


  [HarmonyPatch(nameof(DungeonGenerator.AddOpenConnections)), HarmonyPrefix]
  static void AddOpenConnections(Room newRoom)
  {
    if (DungeonObjects.CurrentRoom == null) return;
    // Blueprints already have correct parameters.
    if (DungeonDB.instance.GetRoom(newRoom.GetHash()) == null) return;
    if (!RoomSpawning.Data.TryGetValue(DungeonObjects.CurrentRoom, out var data)) return;
    // The name must be changed to allow Objects field to work.
    // The hash is used to save the room and handled with RoomSaving patch.
    newRoom.name = data.name;
    var connTo = newRoom.GetConnections();
    for (var i = 0; i < data.connections.Length && i < connTo.Length; ++i)
    {
      var connData = data.connections[i];
      var cTo = connTo[i];
      cTo.m_type = connData.type.StartsWith(">") ? connData.type.Substring(1) : connData.type;
      cTo.m_entrance = connData.entrance;
      cTo.m_allowDoor = connData.door == "true";
      cTo.m_doorOnlyIfOtherAlsoAllowsDoor = connData.door == "other";
    }
  }
}
