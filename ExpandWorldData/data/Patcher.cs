using HarmonyLib;

namespace ExpandWorldData;

public static class DataPatcher
{
  public static void Patch(Harmony harmony)
  {
    PatchNoBuild(harmony, NoBuildManager.HasData || BiomeManager.NoBuildBiomes != 0 || TerritoryManager.HasNoBuild);
    PatchStatusEffects(harmony, BiomeManager.HasStatusEffects || TerritoryManager.HasStatusEffects || EnvironmentManager.HasStatusEffects);
    LocationClientData.Patch(harmony);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.Load), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.LoadOld), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, Helper.IsServer(), typeof(ZoneSystem), "set_" + nameof(ZoneSystem.LocationsGenerated), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeGeneratedLocations), HarmonyPatchType.Postfix);
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