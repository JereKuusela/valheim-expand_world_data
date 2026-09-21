using HarmonyLib;

namespace ExpandWorldData.Vegetation;

public static class Patcher
{
  private static bool IsPatched;

  public static void Patch(Harmony harmony)
  {
    var shouldPatch = Configuration.DataVegetation;
    if (shouldPatch == IsPatched) return;
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.InitializePlacement), HarmonyPatchType.Prefix);
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.ReplacePlacementOperations), HarmonyPatchType.Transpiler);
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.InsideClearArea), typeof(VegetationSpawning), nameof(VegetationSpawning.OverrideClearAreaCheck), HarmonyPatchType.Prefix);
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.ValidateVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.SkipVanillaValidation), HarmonyPatchType.Prefix);
    IsPatched = shouldPatch;
  }

  private static void SetPatch(Harmony harmony, bool shouldPatch, System.Type originalType, string originalName, System.Type patchType, string patchName, HarmonyPatchType type)
  {
    var original = AccessTools.Method(originalType, originalName);
    var patch = AccessTools.Method(patchType, patchName);
    if (shouldPatch)
    {
      var harmonyPatch = new HarmonyMethod(patch);
      if (type == HarmonyPatchType.Prefix) harmony.Patch(original, prefix: harmonyPatch);
      else harmony.Patch(original, transpiler: harmonyPatch);
    }
    else harmony.Unpatch(original, patch);
  }
}