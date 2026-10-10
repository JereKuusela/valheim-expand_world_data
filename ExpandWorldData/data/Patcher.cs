using HarmonyLib;

namespace ExpandWorldData;

public static class DataPatcher
{
  public static void Patch(Harmony harmony)
  {
    PatchNoBuild(harmony, NoBuildManager.HasData || BiomeManager.NoBuildBiomes != 0 || TerritoryManager.HasNoBuild);
    PatchStatusEffects(harmony, BiomeManager.HasStatusEffects || TerritoryManager.HasStatusEffects || EnvironmentManager.HasStatusEffects);
    LocationClientData.Patch(harmony);
    PatchLocationIcons(harmony);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.Load), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.LoadOld), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), "set_" + nameof(ZoneSystem.LocationsGenerated), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeGeneratedLocations), HarmonyPatchType.Postfix);
  }

  internal static void PatchLocationIcons(Harmony harmony)
  {
    // Only servers build icons from location instances. Clients must keep reading the RPC cache.
    var server = Helper.IsServer();
    Patches.Apply(harmony, server, typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcon), typeof(ZoneSystemPatches), nameof(ZoneSystemPatches.GetLocationIcon), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, server && Configuration.DataLocation, typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcons), typeof(ZoneSystemPatches), nameof(ZoneSystemPatches.GetLocationIcons), HarmonyPatchType.Prefix);

    // Remote clients receive icon tokens without loading LocationManager data locally.
    // Keep the renderer and its size-cache lifecycle available regardless of HasData or server role.
    Patches.Apply(harmony, true, typeof(Minimap), nameof(Minimap.GetLocationIcon), typeof(MinimapIcon), nameof(MinimapIcon.NewLocationIcons), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, true, typeof(Minimap), nameof(Minimap.UpdateLocationPins), typeof(MinimapIcon), nameof(MinimapIcon.IconSizeSetup), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, true, typeof(Minimap), nameof(Minimap.Awake), typeof(MinimapIcon), nameof(MinimapIcon.ClearSizes), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, true, typeof(Minimap), nameof(Minimap.UpdatePins), typeof(MinimapIcon), nameof(MinimapIcon.ApplyIconSize), HarmonyPatchType.Transpiler);
  }

  private static void PatchNoBuild(Harmony harmony, bool shouldPatch)
  {
    Patches.Apply(harmony, shouldPatch, typeof(Location), nameof(Location.IsInsideNoBuildLocation), typeof(NoBuildManager), nameof(NoBuildManager.CheckAdditionalZones), HarmonyPatchType.Postfix);
  }

  private static void PatchStatusEffects(Harmony harmony, bool shouldPatch)
  {
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.UpdateEnvStatusEffects), typeof(StatusManager), nameof(StatusManager.UpdateStatusEffects), HarmonyPatchType.Postfix, onUnpatch: StatusManager.CleanUp);
  }

}
