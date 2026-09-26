using HarmonyLib;

namespace ExpandWorldData.Features;

public static class Patcher
{
  private const float DefaultWiggleFrequency = 20f;
  public static bool WorldEnabled { get; private set; }
  public static bool BiomeEnabled { get; private set; }

  public static void SetWorldEnabled(bool enabled) => WorldEnabled = enabled;
  public static void SetBiomeEnabled(bool enabled) => BiomeEnabled = enabled;

  public static void Patch(Harmony harmony)
  {
    GetAshlandsHeight.Patch(harmony, Configuration.AshlandsWidthRestriction, Configuration.AshlandsLengthRestriction);
    CreateAshlandsGap.Patch(harmony, !Configuration.AshlandsGap);
    CreateDeepNorthGap.Patch(harmony, !Configuration.DeepNorthGap);
    Patches.Apply(harmony, !Configuration.ZoneSpawners, typeof(ZoneSystem), nameof(ZoneSystem.PlaceZoneCtrl), typeof(PlaceZoneCtrl), nameof(PlaceZoneCtrl.SkipZoneControlPlacement), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, false, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, false, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateLegacyBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, WorldEnabled && !Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, WorldEnabled && Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateLegacyBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, WorldEnabled, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), typeof(GetAshlandsOceanGradient), nameof(GetAshlandsOceanGradient.CalculateGradient), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float)]);
    Patches.Apply(harmony, Configuration.WiggleFrequency != DefaultWiggleFrequency, typeof(WorldGenerator), nameof(WorldGenerator.WorldAngle), typeof(WorldAngle), nameof(WorldAngle.CalculateAngle), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float)]);
    PatchNature(harmony);
    PatchWaterColor(harmony);
  }

  private static void PatchNature(Harmony harmony)
  {
    var shouldPatch = BiomeEnabled && BiomeManager.HasNatureOverrides;
    Patches.Apply(harmony, shouldPatch, typeof(Beehive), nameof(Beehive.CheckBiome), typeof(BeehiveCheckBiome), nameof(BeehiveCheckBiome.BeginNatureBiomeCheck), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Beehive), nameof(Beehive.CheckBiome), typeof(BeehiveCheckBiome), nameof(BeehiveCheckBiome.EndNatureBiomeCheck), HarmonyPatchType.Finalizer);
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.UpdatePlacementGhost), typeof(PlayerUpdatePlacementGhost), nameof(PlayerUpdatePlacementGhost.BeginNatureBiomeCheck), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.UpdatePlacementGhost), typeof(PlayerUpdatePlacementGhost), nameof(PlayerUpdatePlacementGhost.EndNatureBiomeCheck), HarmonyPatchType.Finalizer);
    Patches.Apply(harmony, shouldPatch, typeof(Plant), nameof(Plant.UpdateHealth), typeof(PlantUpdateHealth), nameof(PlantUpdateHealth.BeginNatureBiomeCheck), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Plant), nameof(Plant.UpdateHealth), typeof(PlantUpdateHealth), nameof(PlantUpdateHealth.EndNatureBiomeCheck), HarmonyPatchType.Finalizer);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetGroundMaterial), typeof(HeightmapGetGroundMaterial), nameof(HeightmapGetGroundMaterial.BeginNatureBiomeCheck), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetGroundMaterial), typeof(HeightmapGetGroundMaterial), nameof(HeightmapGetGroundMaterial.EndNatureBiomeCheck), HarmonyPatchType.Finalizer);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetBiome), typeof(GetBiomeHM), nameof(GetBiomeHM.ApplyNatureBiome), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.FindBiome), typeof(HeightmapFindBiome), nameof(HeightmapFindBiome.ApplyNatureBiome), HarmonyPatchType.Postfix);
  }

  private static void PatchWaterColor(Harmony harmony)
  {
    var shouldPatch = Configuration.CustomWaterColor;
    var callback = nameof(WaterColor.StartBiomeTransition);
    if (!shouldPatch && Patches.IsRegistered(typeof(WaterColor), callback)) WaterColor.StopTransition();
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.AddKnownBiome), typeof(WaterColor), nameof(WaterColor.StartBiomeTransition), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.OnSpawned), typeof(WaterColor), nameof(WaterColor.ResetTransition), HarmonyPatchType.Postfix);
  }
}