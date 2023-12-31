using System;
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
  // Note: OverrideRoomData will change the parameters of the room prefab. Don't use them for anything.
  public static Dictionary<string, DungeonDB.RoomData> Prefabs = [];

  public static Dictionary<string, RoomData> Data = [];

  public static Room OverrideParameters(Room from, Room to)
  {
    // The name must be changed to allow Objects field to work.
    // The hash is used to save the room and handled with RoomSaving patch.
    to.name = from.name;
    to.m_theme = from.m_theme;
    to.m_entrance = from.m_entrance;
    to.m_endCap = from.m_endCap;
    to.m_divider = from.m_divider;
    to.m_enabled = from.m_enabled;
    to.m_size = from.m_size;
    to.m_minPlaceOrder = from.m_minPlaceOrder;
    to.m_weight = from.m_weight;
    to.m_faceCenter = from.m_faceCenter;
    to.m_perimeter = from.m_perimeter;
    to.m_endCapPrio = from.m_endCapPrio;
    to.m_perimeter = from.m_perimeter;
    var connFrom = from.GetConnections();
    var connTo = to.GetConnections();
    for (var i = 0; i < connFrom.Length && i < connTo.Length; ++i)
    {
      var cFrom = connFrom[i];
      var cTo = connTo[i];
      cTo.transform.localPosition = cFrom.transform.localPosition;
      cTo.transform.localRotation = cFrom.transform.localRotation;
      cTo.m_type = cFrom.m_type;
      cTo.m_entrance = cFrom.m_entrance;
      cTo.m_allowDoor = cFrom.m_allowDoor;
      cTo.m_doorOnlyIfOtherAlsoAllowsDoor = cFrom.m_doorOnlyIfOtherAlsoAllowsDoor;
    }
    return to;
  }

  private static bool IsBaseRoom(Room room)
  {
    var baseName = Parse.Name(room.name);
    return Prefabs.ContainsKey(baseName);
  }

  [HarmonyPatch(nameof(DungeonGenerator.SetupAvailableRooms)), HarmonyPostfix]
  static void SetupAvailableRooms()
  {
    if (Helper.IsClient()) return;
    // To support live reloading for blueprints, the connections must be refreshed every time.
    foreach (var roomData in DungeonGenerator.m_availableRooms)
    {
      var room = roomData.m_room;
      if (IsBaseRoom(room)) continue;
      if (BlueprintManager.TryGet(room.name, out var bp))
      {
        if (Data.TryGetValue(room.name, out var data) && data.size == "")
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
    // Blueprints add a dummy room which shouldn't be saved.
    DungeonGenerator.m_placedRooms = DungeonGenerator.m_placedRooms.Where(IsBaseRoom).ToList();
    // Restore base names to save the rooms as vanilla compatible.
    foreach (var room in DungeonGenerator.m_placedRooms)
      room.name = Parse.Name(room.name);
  }

}
