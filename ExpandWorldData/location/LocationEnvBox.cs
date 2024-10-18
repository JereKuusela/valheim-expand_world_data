
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData.Dungeon;


// Client side tweak to make the dungeon environment box extend to the whole dungeon.
[HarmonyPatch]
public class EnvironmentBox
{

  // Not fully sure if the generator or location loads first.
  public static Dictionary<Vector2i, Vector3> Cache = [];


  private static void TryScale(Location loc)
  {
    var zone = ZoneSystem.GetZone(loc.transform.position);
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
    var zone = ZoneSystem.GetZone(pos);
    var center = ZoneSystem.GetZonePos(zone) with { y = pos.y };
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
    var locsInZone = Location.s_allLocations.Where(loc => ZoneSystem.GetZone(loc.transform.position) == zone).ToArray();
    foreach (var loc in locsInZone)
      TryScale(loc);
  }

  [HarmonyPatch(typeof(Location), nameof(Location.Awake)), HarmonyPostfix]
  static void ScaleEnvironmentBox2(Location __instance)
  {
    TryScale(__instance);
  }
}

/*

// Client side tweak to make the dungeon environment box extend to the whole dungeon.
[HarmonyPatch]
public class EnvironmentBox
{
  private static readonly int HashBounds = "bounds".GetHashCode();
  private static readonly Dictionary<Vector2i, Vector3> DungeonSizes = [];

  [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate), typeof(ZoneSystem.SpawnMode)), HarmonyPostfix]
  static void SaveDungeonSize(DungeonGenerator __instance)
  {
    var zone = ZoneSystem.instance.GetZone(__instance.transform.position);
    DungeonSizes[zone] = __instance.m_zoneSize;
  }


  [HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SetLocation))]
  public class SaveBounds
  {
    static void Prefix(LocationProxy __instance)
    {
      var zone = ZoneSystem.instance.GetZone(__instance.transform.position);
      if (DungeonSizes.TryGetValue(zone, out var size))
      {
        var view = __instance.GetComponent<ZNetView>();
        if (view) view.GetZDO().Set(HashBounds, size);
      }
    }
  }

  // Previously bounds were dynamically calculated from the room colliders.
  // So convert to the new system where the bound is directly saved.
  [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Load)), HarmonyPostfix]
  static void LegacySaveBounds(DungeonGenerator __instance)
  {
    var pos = __instance.transform.position;
    var zone = ZoneSystem.instance.GetZone(pos);
    var locsInZone = Location.m_allLocations.Where(loc => ZoneSystem.instance.GetZone(loc.transform.position) == zone).ToArray();
    if (locsInZone.Any(HasBounds)) return;

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
    var size = 2 * extents;
    foreach (var loc in locsInZone)
    {
      var envZone = loc.GetComponentInChildren<EnvZone>();
      if (!envZone) continue;
      var view = loc.GetComponent<ZNetView>();
      if (!view) continue;
      var origSize = envZone.transform.localScale;
      // Don't shrink from the default so that people can build there more easily.
      // Otherwise for small dungeons the box would be very small.
      size.x = Mathf.Max(size.x, origSize.x);
      size.y = Mathf.Max(size.y, origSize.y);
      size.z = Mathf.Max(size.z, origSize.z);
      view.GetZDO().Set(HashBounds, 2 * extents);
      ScaleEnvironmentBox(loc);
    }
  }
  [HarmonyPatch(typeof(Location), nameof(Location.Awake)), HarmonyPostfix]
  static void ScaleEnvironmentBox(Location __instance)
  {
    var bounds = GetBounds(__instance);
    if (bounds == Vector3.zero) return;
    var envZone = __instance.GetComponentInChildren<EnvZone>();
    if (!envZone) return;
    var zone = ZoneSystem.instance.GetZone(__instance.transform.position);
    DungeonSizes[zone] = bounds;
    envZone.transform.localScale = bounds;
  }
  static Vector3 GetBounds(Location __instance)
  {
    var view = __instance.GetComponent<ZNetView>();
    if (!view) return Vector3.zero;
    return view.GetZDO().GetVec3(HashBounds, Vector3.zero);
  }
  static bool HasBounds(Location __instance)
  {
    var view = __instance.GetComponent<ZNetView>();
    if (!view) return false;
    return view.GetZDO().GetVec3(HashBounds, Vector3.zero) != Vector3.zero;
  }
}*/