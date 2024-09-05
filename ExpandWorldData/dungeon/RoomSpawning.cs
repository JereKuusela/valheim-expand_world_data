using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Service;

namespace ExpandWorldData;

// Note: 
// Ghost init is already set by ZoneSystem.SpawnObject.
// So no need to check here for it.

// Room variants are tricky to implement because the room prefab and room parameters are in the same component.
// New entries can't be directly created because the room prefab can't be copied.
// So the idea is to separate the prefab and parameters by creating a room proxy for each room.
// When the room is selected, the actual room component is built from the base room prefab and the proxy parameters.
[HarmonyPatch(typeof(DungeonGenerator))]
public class RoomSpawning
{
  public static Dictionary<string, DungeonDB.RoomData> Prefabs = [];

  public static Dictionary<Room, Vector3> RoomSizes = [];
  public static Dictionary<DungeonDB.RoomData, RoomData> Data = [];
  public static Dictionary<DungeonDB.RoomData, string> Blueprints = [];

  private static bool IsBaseRoom(DungeonDB.RoomData room)
  {
    var baseName = Parse.Name(room.m_prefab.Name);
    return Prefabs.ContainsKey(baseName);
  }

  [HarmonyPatch(nameof(DungeonGenerator.SetupAvailableRooms)), HarmonyPostfix]
  static void SetupAvailableRooms()
  {
    if (Helper.IsClient()) return;
    // To support live reloading for blueprints, the connections must be refreshed every time.
    foreach (var roomData in DungeonGenerator.m_availableRooms)
    {
      if (!Blueprints.TryGetValue(roomData, out var bpName))
        continue;
      var room = roomData.RoomInPrefab;
      if (BlueprintManager.TryGet(bpName, out var bp))
      {
        if (Data.TryGetValue(roomData, out var data) && data.size == "")
          room.m_size = new((int)Mathf.Ceil(bp.Size.x), (int)Mathf.Ceil(bp.Size.y), (int)Mathf.Ceil(bp.Size.z));
        for (var i = 0; i < bp.SnapPoints.Count && i < room.m_roomConnections.Length; ++i)
        {
          var conn = room.m_roomConnections[i];
          conn.transform.localPosition = bp.SnapPoints[i].Pos;
          conn.transform.localRotation = bp.SnapPoints[i].Rot;
        }
      }
    }
  }

  [HarmonyPatch(nameof(DungeonGenerator.Save)), HarmonyPrefix]
  static void CleanRoomsForSaving()
  {
    if (Prefabs.Count == 0 || Helper.IsClient()) return;
    // Restore base names to save the rooms as vanilla compatible.
    foreach (var room in DungeonGenerator.m_placedRooms)
      room.name = Parse.Name(room.name);
    // Blueprints add a dummy room which shouldn't be saved.
    DungeonGenerator.m_placedRooms = DungeonGenerator.m_placedRooms.Where(r => DungeonDB.instance.GetRoom(r.GetHash()) != null).ToList();
  }


  [HarmonyPatch(nameof(DungeonGenerator.TestCollision)), HarmonyPrefix]
  static bool TestCollisionCustom(DungeonGenerator __instance, Room room, Vector3 pos, Quaternion rot, ref bool __result)
  {
    if (!RoomSizes.TryGetValue(room, out var size)) return true;
    __result = TestCollision(__instance, pos, rot, size);
    return false;
  }

  private static bool TestCollision(DungeonGenerator dg, Vector3 pos, Quaternion rot, Vector3 size)
  {
    if (!IsInsideDungeon(dg, pos, rot, size)) return true;
    dg.m_colliderA.size = new Vector3(size.x - 0.1f, size.y - 0.1f, size.z - 0.1f);
    foreach (Room room2 in DungeonGenerator.m_placedRooms)
    {
      dg.m_colliderB.size = RoomSizes.TryGetValue(room2, out var s) ? s : room2.m_size;
      if (Physics.ComputePenetration(dg.m_colliderA, pos, rot, dg.m_colliderB, room2.transform.position, room2.transform.rotation, out _, out _))
        return true;
    }
    return false;
  }
  private static bool IsInsideDungeon(DungeonGenerator dg, Vector3 pos, Quaternion rot, Vector3 size)
  {
    Bounds bounds = new Bounds(dg.m_zoneCenter, dg.m_zoneSize);
    size *= 0.5f;
    return bounds.Contains(pos + rot * new Vector3(size.x, size.y, -size.z)) && bounds.Contains(pos + rot * new Vector3(-size.x, size.y, -size.z)) && bounds.Contains(pos + rot * new Vector3(size.x, size.y, size.z)) && bounds.Contains(pos + rot * new Vector3(-size.x, size.y, size.z)) && bounds.Contains(pos + rot * new Vector3(size.x, -size.y, -size.z)) && bounds.Contains(pos + rot * new Vector3(-size.x, -size.y, -size.z)) && bounds.Contains(pos + rot * new Vector3(size.x, -size.y, size.z)) && bounds.Contains(pos + rot * new Vector3(-size.x, -size.y, size.z));
  }

}
