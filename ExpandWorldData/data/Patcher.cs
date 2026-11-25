using ExpandWorldData.Biome;
using ExpandWorldData.Environment;
using ExpandWorldData.Territory;
using HarmonyLib;

namespace ExpandWorldData.Data;

public static class DataPatcher
{
  public static void Patch(Harmony harmony)
  {
    PatchNoBuild(harmony, !Configuration.ServerOnly && (NoBuildManager.HasData || BiomeManager.NoBuildBiomes != 0 || TerritoryManager.HasNoBuild));
    PatchStatusEffects(harmony, BiomeManager.HasStatusEffects || TerritoryManager.HasStatusEffects || EnvironmentManager.HasStatusEffects);
    Patches.Postfix(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.Load), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData));
    Patches.Postfix(harmony, Helper.IsServer(), typeof(ZoneSystem), nameof(ZoneSystem.LoadOld), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData));
    Patches.Postfix(harmony, Helper.IsServer(), typeof(ZoneSystem), "set_" + nameof(ZoneSystem.LocationsGenerated), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeGeneratedLocations));
  }

  private static void PatchNoBuild(Harmony harmony, bool shouldPatch)
  {
    Patches.Postfix(harmony, shouldPatch, typeof(Location), nameof(Location.IsInsideNoBuildLocation), typeof(NoBuildManager), nameof(NoBuildManager.CheckAdditionalZones), Priority.HigherThanNormal);
  }

  private static void PatchStatusEffects(Harmony harmony, bool shouldPatch)
  {
    Patches.Postfix(harmony, shouldPatch, typeof(Player), nameof(Player.UpdateEnvStatusEffects), typeof(StatusManager), nameof(StatusManager.UpdateStatusEffects), onUnpatch: StatusManager.CleanUp);
  }

}
