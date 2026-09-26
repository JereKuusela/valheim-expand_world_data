using HarmonyLib;
using ExpandWorldData;

namespace ExpandWorldData.Vegetation;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var shouldPatch = Configuration.DataVegetation;
    Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.InitializePlacement), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.ReplacePlacementOperations), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.InsideClearArea), typeof(VegetationSpawning), nameof(VegetationSpawning.OverrideClearAreaCheck), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.ValidateVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.SkipVanillaValidation), HarmonyPatchType.Prefix);
  }
}