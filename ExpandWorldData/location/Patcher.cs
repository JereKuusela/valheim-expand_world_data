using System.Diagnostics;
using System.Linq;
using ExpandWorldData.Biome;
using ExpandWorldData.Features;
using HarmonyLib;

namespace ExpandWorldData.Locations;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    LocationClientData.Patch(harmony);
    PatchIcons(harmony);

    var hasData = LocationManager.Customized;
    Patches.Prefix(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.PlaceLocations), typeof(PrepareTerrainBeforeZoneContents), nameof(PrepareTerrainBeforeZoneContents.Prefix));
    Patches.Prefix(harmony, hasData, typeof(ZoneSystem), nameof(ZoneSystem.CreateLocationProxy), typeof(LocationZDO), nameof(LocationZDO.Prefix));
    Patches.Transpiler(harmony, Configuration.ScaleLocationAltitudeRequirement && BiomeManager.HasData, typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocationsTimeSliced), typeof(ScaleLocationHeightRequirement), nameof(ScaleLocationHeightRequirement.TranspileMoveNext), argumentTypes: [typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage)], enumerator: true);
    Patches.Postfix(harmony, hasData, typeof(ZoneSystem), nameof(ZoneSystem.CreateLocalZones), typeof(CreateLocalZones), nameof(CreateLocalZones.Postfix));
    Patches.Prefix(harmony, hasData, typeof(ZoneSystem), nameof(ZoneSystem.HaveLocationInRange), typeof(HaveLocationInRange), nameof(HaveLocationInRange.Prefix));
    Patches.Prefix(harmony, hasData, typeof(ZoneSystem), nameof(ZoneSystem.GetLocation), typeof(ZoneSystemPatches), nameof(ZoneSystemPatches.GetLocation), argumentTypes: [typeof(string)]);
    var hasClearAreaOverride = LocationExtra.ExtraInfo.Values.Any(extra => extra.ClearAreaRadius > 0f);
    Patches.Transpiler(harmony, hasClearAreaOverride, typeof(ZoneSystem), nameof(ZoneSystem.PlaceLocations), typeof(LocationClearArea), nameof(LocationClearArea.Transpiler));
  }

  internal static void PatchIcons(Harmony harmony)
  {
    // Only servers build icons from location instances. Clients must keep reading the RPC cache.
    var server = Helper.IsServer();
    Patches.Prefix(harmony, server, typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcon), typeof(ZoneSystemPatches), nameof(ZoneSystemPatches.GetLocationIcon));
    Patches.Prefix(harmony, server && Configuration.DataLocation, typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcons), typeof(ZoneSystemPatches), nameof(ZoneSystemPatches.GetLocationIcons));

    // Remote clients receive icon tokens without loading LocationManager data locally.
    // Keep the renderer and its size-cache lifecycle available regardless of HasData or server role.
    Patches.Postfix(harmony, true, typeof(Minimap), nameof(Minimap.GetLocationIcon), typeof(MinimapIcon), nameof(MinimapIcon.NewLocationIcons));
    Patches.Transpiler(harmony, true, typeof(Minimap), nameof(Minimap.UpdateLocationPins), typeof(MinimapIcon), nameof(MinimapIcon.IconSizeSetup));
    Patches.Postfix(harmony, true, typeof(Minimap), nameof(Minimap.Awake), typeof(MinimapIcon), nameof(MinimapIcon.ClearSizes));
    Patches.Transpiler(harmony, true, typeof(Minimap), nameof(Minimap.UpdatePins), typeof(MinimapIcon), nameof(MinimapIcon.ApplyIconSize));
  }
}