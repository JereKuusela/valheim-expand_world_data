using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ExpandWorldData.Locations;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData.Features;

[HarmonyPatch(typeof(LoadingIndicator), nameof(LoadingIndicator.SetShowProgress))]
public class GuaranteeLocations
{
  static void GuaranteeStartLocation(ZoneSystem zs)
  {
    var location = zs.m_locations.FirstOrDefault(loc => loc.m_prefab.name == Game.instance.m_StartLocation);
    if (!location.m_enable || location.m_quantity == 0)
      return;
    if (Count(zs, location) == 0)
    {
      Log.Info($"Forcefully placing {location.m_prefab.name} location at the center.");
      var locationRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
      Vector3 randomPointInZone = ZoneSystem.GetRandomPointInZone(Vector2s.zero, locationRadius);
      zs.RegisterLocation(location, randomPointInZone, false);
    }
  }
  static int Count(ZoneSystem zs, ZoneSystem.ZoneLocation location) => zs.m_locationInstances.Values.Count(loc => loc.m_location.m_prefab.name == location.m_prefab.name);
  static void Finalizer(bool show)
  {
    if (show) return;
    GuaranteeStartLocation(ZoneSystem.instance);
    CreateLocalZones.LocationsPregenerated = false;
  }
}

public class PlaceZoneCtrl
{
  internal static bool SkipZoneControlPlacement() => false;
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation))]
public class ClearAreasFromAdjacentZones
{
  static void Prefix(ZoneSystem __instance, Vector2i zoneID, List<ZoneSystem.ClearArea> clearAreas)
  {
    for (var i = zoneID.x - 1; i <= zoneID.x + 1; ++i)
    {
      for (var j = zoneID.y - 1; j <= zoneID.y + 1; j++)
      {
        // Current zone alredy handled.
        if (i == zoneID.x && j == zoneID.y) continue;
        // No location in the zone.
        if (!__instance.m_locationInstances.TryGetValue(new(i, j), out var item)) continue;
        // Check for corrupted locations.
        if (item.m_location == null) continue;
        // No clear area in the location.
        if (!item.m_location.m_clearArea) continue;
        var radius = LocationClearArea.GetRadius(item.m_location);
        if (radius < 32f && !LocationClearArea.HasOverride(item.m_location)) continue;
        clearAreas.Add(new(item.m_position, radius));
      }
    }
  }
}

public static class LocationClearArea
{
  internal static bool HasOverride(ZoneSystem.ZoneLocation location) =>
    LocationExtra.TryGet(location, out var extra) && extra.ClearAreaRadius.HasValue;

  internal static float GetRadius(ZoneSystem.ZoneLocation location) =>
    LocationExtra.TryGet(location, out var extra) ? extra.ClearAreaRadius ?? location.m_exteriorRadius : location.m_exteriorRadius;

  internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var radius = AccessTools.Field(typeof(ZoneSystem.ZoneLocation), nameof(ZoneSystem.ZoneLocation.m_exteriorRadius));
    var constructor = AccessTools.Constructor(typeof(ZoneSystem.ClearArea), [typeof(Vector3), typeof(float)]);
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, radius), new CodeMatch(OpCodes.Newobj, constructor))
      .Set(OpCodes.Call, AccessTools.Method(typeof(LocationClearArea), nameof(GetRadius)))
      .InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetRandomPointInZone), typeof(Vector2s), typeof(float))]
public class FixGetRandomPointInZone
{
  static Vector3 Postfix(Vector3 result, Vector2s zone, float locationRadius)
  {
    // If fits inside the zone, vanilla code works.
    if (locationRadius < 32f) return result;
    // Otherwise hardcode at the zone center to ensure the best fit.
    return ZoneSystem.GetZonePos(zone);
  }
}
