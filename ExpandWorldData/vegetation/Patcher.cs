using HarmonyLib;
using ExpandWorldData;

namespace ExpandWorldData.Vegetation;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var shouldPatch = VegetationManager.Customized;
    Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.InitializePlacement));
    Patches.Transpiler(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.ReplacePlacementOperations));
    Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.InsideClearArea), typeof(VegetationSpawning), nameof(VegetationSpawning.OverrideClearAreaCheck));
    Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.ValidateVegetation), typeof(VegetationSpawning), nameof(VegetationSpawning.SkipVanillaValidation));
  }
}