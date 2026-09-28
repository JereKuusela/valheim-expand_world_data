using HarmonyLib;

namespace ExpandWorldData.Features;

public static class Patcher
{
  private const float DefaultWiggleFrequency = 20f;

  public static void Patch(Harmony harmony)
  {
    var worldData = WorldManager.HasData;
    var restrictionsChanged = Configuration.AshlandsWidthRestriction != GetAshlandsHeight.DefaultWidthRestriction || Configuration.AshlandsLengthRestriction != GetAshlandsHeight.DefaultLengthRestriction;
    Patches.Apply(harmony, restrictionsChanged, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight), typeof(GetAshlandsHeight), nameof(GetAshlandsHeight.ReplaceRestrictions), HarmonyPatchType.Transpiler, state: (Configuration.AshlandsWidthRestriction, Configuration.AshlandsLengthRestriction));
    Patches.Apply(harmony, !Configuration.AshlandsGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap), typeof(DisableGap), nameof(DisableGap.SkipGap), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, !Configuration.DeepNorthGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap), typeof(DisableGap), nameof(DisableGap.SkipGap), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, !Configuration.ZoneSpawners, typeof(ZoneSystem), nameof(ZoneSystem.PlaceZoneCtrl), typeof(PlaceZoneCtrl), nameof(PlaceZoneCtrl.SkipZoneControlPlacement), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, worldData && !Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, worldData && Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateLegacyBiome), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Apply(harmony, worldData, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), typeof(GetAshlandsOceanGradient), nameof(GetAshlandsOceanGradient.CalculateGradient), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float)]);
    Patches.Apply(harmony, worldData, typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands), typeof(BiomeHeat), nameof(BiomeHeat.IsAshlands), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, Configuration.WiggleFrequency != DefaultWiggleFrequency, typeof(WorldGenerator), nameof(WorldGenerator.WorldAngle), typeof(WorldAngle), nameof(WorldAngle.CalculateAngle), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float)]);
    PatchAltBiomes(harmony, worldData);
    PatchNature(harmony);
    PatchLava(harmony);
    PatchTerrain(harmony);
    PatchWaterColor(harmony);
  }

  private static void PatchAltBiomes(Harmony harmony, bool worldData)
  {
    var shouldPatch = worldData && BiomeCalculator.HasAltBiomeData;
    Patches.Apply(harmony, shouldPatch, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(AltBiomePlacement), nameof(AltBiomePlacement.PrepareWorldAltBiomes), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(AltBiomePlacement), nameof(AltBiomePlacement.ApplyWorldAltBiomes), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, shouldPatch, typeof(BiomeSector), nameof(BiomeSector.CanAddModifier), typeof(AltBiomePlacement), nameof(AltBiomePlacement.BlockForcedSectors), HarmonyPatchType.Prefix);
  }

  private static void PatchNature(Harmony harmony)
  {
    var shouldPatch = BiomeManager.HasData && BiomeManager.HasNatureOverrides;
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

  private static void PatchLava(Harmony harmony)
  {
    var shouldPatch = BiomeManager.HasLavaOverrides;
    Patches.Apply(harmony, shouldPatch, typeof(Character), nameof(Character.UpdateLava), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLava), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetLava), typeof(BiomeHeat), nameof(BiomeHeat.ReplaceGetLava), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.IsLava), typeof(BiomeHeat), nameof(BiomeHeat.ReplaceIsLava), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetHeightOffset), typeof(BiomeHeat), nameof(BiomeHeat.GetHeightOffset), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.ScanForLava), typeof(BiomeHeat), nameof(BiomeHeat.ScanForLava), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbient), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLavaAmbient), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbientLoops), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLavaAmbientLoops), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.IsLavaPreHeightmap), typeof(BiomeHeat), nameof(BiomeHeat.IsLavaPreHeightmap), HarmonyPatchType.Transpiler);
  }

  private static void PatchTerrain(Harmony harmony)
  {
    var biomeData = BiomeManager.HasData;
    var territoryData = TerritoryManager.HasData;
    var anyData = biomeData || territoryData;
    Patches.Apply(harmony, anyData, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(BiomeHeight), nameof(BiomeHeight.ReplaceTerrain), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, anyData, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(BiomeHeight), nameof(BiomeHeight.ModifyHeight), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, anyData, typeof(Minimap), nameof(Minimap.GetPixelColor), typeof(GetPixelColor), nameof(GetPixelColor.ApplyCustomColor), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, biomeData, typeof(Minimap), nameof(Minimap.GetMaskColor), typeof(GetMaskColor), nameof(GetMaskColor.ApplyTerrain), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, biomeData, typeof(Minimap), nameof(Minimap.GenerateWorldMap), typeof(GenerateWorldMapHeight), nameof(GenerateWorldMapHeight.ApplyMapColorMultiplier), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, biomeData, typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(HeightmapPatches), nameof(HeightmapPatches.GetBiomeColor), HarmonyPatchType.Prefix, argumentTypes: [typeof(Heightmap.Biome)]);
    Patches.Apply(harmony, territoryData, typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(HeightmapPatches), nameof(HeightmapPatches.CustomGetBiomeColor), HarmonyPatchType.Prefix, argumentTypes: [typeof(float), typeof(float)]);
    Patches.Apply(harmony, territoryData, typeof(Heightmap), nameof(Heightmap.OnDestroy), typeof(HeightmapPatches), nameof(HeightmapPatches.RemoveCornerTerritories), HarmonyPatchType.Postfix, onUnpatch: HeightmapPatches.ClearCornerTerritories);
  }

  private static void PatchWaterColor(Harmony harmony)
  {
    var shouldPatch = Configuration.CustomWaterColor;
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.AddKnownBiome), typeof(WaterColor), nameof(WaterColor.StartBiomeTransition), HarmonyPatchType.Postfix, onUnpatch: WaterColor.StopTransition);
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.OnSpawned), typeof(WaterColor), nameof(WaterColor.ResetTransition), HarmonyPatchType.Postfix);
  }
}