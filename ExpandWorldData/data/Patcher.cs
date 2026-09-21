using HarmonyLib;

namespace ExpandWorldData;

public static class DataPatcher
{
  private static bool ProducerPatched;
  private static bool NoBuildPatched;
  private static bool StatusEffectsPatched;

  public static void Patch(Harmony harmony)
  {
    PatchProducer(harmony);
    PatchNoBuild(harmony, NoBuildManager.HasData || BiomeManager.NoBuildBiomes != 0 || TerritoryManager.HasNoBuild);
    PatchStatusEffects(harmony, BiomeManager.HasStatusEffects || TerritoryManager.HasStatusEffects || EnvironmentManager.HasStatusEffects);
  }

  private static void PatchProducer(Harmony harmony)
  {
    if (ProducerPatched) return;
    SetPatch(harmony, true, typeof(ZoneSystem), nameof(ZoneSystem.Load), typeof(NoBuildManager), nameof(NoBuildManager.SynchronizeLocationData));
    ProducerPatched = true;
  }

  private static void PatchNoBuild(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == NoBuildPatched) return;
    SetPatch(harmony, shouldPatch, typeof(Location), nameof(Location.IsInsideNoBuildLocation), typeof(NoBuildManager), nameof(NoBuildManager.CheckAdditionalZones));
    NoBuildPatched = shouldPatch;
  }

  private static void PatchStatusEffects(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == StatusEffectsPatched) return;
    if (!shouldPatch) StatusManager.CleanUp();
    SetPatch(harmony, shouldPatch, typeof(Player), nameof(Player.UpdateEnvStatusEffects), typeof(StatusManager), nameof(StatusManager.UpdateStatusEffects));
    StatusEffectsPatched = shouldPatch;
  }

  private static void SetPatch(Harmony harmony, bool shouldPatch, System.Type originalType, string originalName, System.Type patchType, string patchName)
  {
    var original = AccessTools.Method(originalType, originalName);
    var patch = AccessTools.Method(patchType, patchName);
    if (shouldPatch) harmony.Patch(original, postfix: new HarmonyMethod(patch));
    else harmony.Unpatch(original, patch);
  }
}