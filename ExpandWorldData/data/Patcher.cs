using HarmonyLib;

namespace ExpandWorldData;

public static class DataPatcher
{
  public static void Patch(Harmony harmony)
  {
    PatchProducer(harmony);
    PatchNoBuild(harmony, NoBuildManager.HasData || BiomeManager.NoBuildBiomes != 0 || TerritoryManager.HasNoBuild);
    PatchStatusEffects(harmony, BiomeManager.HasStatusEffects || TerritoryManager.HasStatusEffects || EnvironmentManager.HasStatusEffects);
  }

  private static void PatchProducer(Harmony harmony)
  {
    Patches.Apply(harmony, true, typeof(ZoneSystem), nameof(ZoneSystem.Load), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData), HarmonyPatchType.Postfix);
  }

  private static void PatchNoBuild(Harmony harmony, bool shouldPatch)
  {
    Patches.Apply(harmony, shouldPatch, typeof(Location), nameof(Location.IsInsideNoBuildLocation), typeof(NoBuildManager), nameof(NoBuildManager.CheckAdditionalZones), HarmonyPatchType.Postfix);
  }

  private static void PatchStatusEffects(Harmony harmony, bool shouldPatch)
  {
    var callback = nameof(StatusManager.UpdateStatusEffects);
    if (!shouldPatch && Patches.IsRegistered(typeof(StatusManager), callback)) StatusManager.CleanUp();
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.UpdateEnvStatusEffects), typeof(StatusManager), nameof(StatusManager.UpdateStatusEffects), HarmonyPatchType.Postfix);
  }

}