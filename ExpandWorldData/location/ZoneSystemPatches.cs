using System.Collections.Generic;
using UnityEngine;

namespace ExpandWorldData.Locations;

public static class ZoneSystemPatches
{
  internal static bool GetLocationIcon(ZoneSystem __instance, string name, ref Vector3 pos, ref bool __result)
  {
    // Server should also use GetLocationIcons so that single player matches the dedicated server behavior.
    __instance.tempIconList.Clear();
    __instance.GetLocationIcons(__instance.tempIconList);
    foreach (var kvp in __instance.tempIconList)
    {
      if (kvp.Value != name)
        continue;
      pos = kvp.Key;
      __result = true;
      return false;
    }
    pos = Vector3.zero;
    __result = false;
    return false;
  }

  internal static bool GetLocationIcons(ZoneSystem __instance, Dictionary<Vector3, string> icons)
  {
    foreach (var kvp in __instance.m_locationInstances)
    {
      var loc = kvp.Value.m_location;
      var pos = kvp.Value.m_position;
      if (loc == null) continue;
      if (LocationExtra.TryGetData(loc, out var data))
      {
        var placed = data.iconPlaced == "true" ? loc.m_prefab.Name : data.iconPlaced == "false" ? "" : data.iconPlaced;
        if (kvp.Value.m_placed && placed != "")
        {
          icons[pos] = placed;
        }
        else
        {
          pos.y += 0.00001f; // Trivial amount for a different key.
          var always = data.iconAlways == "true" ? loc.m_prefab.Name : data.iconAlways == "false" ? "" : data.iconAlways;
          if (always != "") icons[pos] = always;
        }
      }
      // Jewelcrafting has dynamic locations so use the vanilla code as a fallback.
      else if (loc.m_iconAlways || (loc.m_iconPlaced && kvp.Value.m_placed))
      {
        icons[pos] = loc.m_prefab.Name;
      }
    }
    return false;
  }

  // Vanilla checks that location prefabs are valid.
  // This doesn't work with blueprints because they would need proper asset ID (which might conflict with other mods).
  internal static bool GetLocation(ZoneSystem __instance, string name, ref ZoneSystem.ZoneLocation __result)
  {
    foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
    {
      if (zoneLocation.m_prefab.Name == name)
      {
        __result = zoneLocation;
        return false;
      }
    }
    __result = null!;
    return false;
  }
}
