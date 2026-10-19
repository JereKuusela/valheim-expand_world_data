using ExpandWorldData.AltBiomes;
using ExpandWorldData.Biome;
using ExpandWorldData.Territory;
using ExpandWorldData.World;
using HarmonyLib;

namespace ExpandWorldData.Features;

public static class Patcher
{
  private const float DefaultWiggleFrequency = 20f;

  public static void Patch(Harmony harmony)
  {
    var worldData = WorldManager.HasData;
    var restrictionsChanged = Configuration.AshlandsWidthRestriction != GetAshlandsHeight.DefaultWidthRestriction || Configuration.AshlandsLengthRestriction != GetAshlandsHeight.DefaultLengthRestriction;
    Patches.Transpiler(harmony, restrictionsChanged, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight), typeof(GetAshlandsHeight), nameof(GetAshlandsHeight.ReplaceRestrictions), state: (Configuration.AshlandsWidthRestriction, Configuration.AshlandsLengthRestriction));
    Patches.Prefix(harmony, !Configuration.AshlandsGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap), typeof(DisableGap), nameof(DisableGap.SkipGap));
    Patches.Prefix(harmony, !Configuration.DeepNorthGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap), typeof(DisableGap), nameof(DisableGap.SkipGap));
    Patches.Prefix(harmony, !Configuration.ZoneSpawners, typeof(ZoneSystem), nameof(ZoneSystem.PlaceZoneCtrl), typeof(PlaceZoneCtrl), nameof(PlaceZoneCtrl.SkipZoneControlPlacement));
    Patches.Prefix(harmony, worldData && !Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateBiome), argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Prefix(harmony, worldData && Configuration.LegacyGeneration, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(GetBiomeWG), nameof(GetBiomeWG.CalculateLegacyBiome), argumentTypes: [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    Patches.Prefix(harmony, worldData, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), typeof(GetAshlandsOceanGradient), nameof(GetAshlandsOceanGradient.CalculateGradient), argumentTypes: [typeof(float), typeof(float)]);
    Patches.Prefix(harmony, worldData, typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands), typeof(BiomeHeat), nameof(BiomeHeat.IsAshlands));
    Patches.Prefix(harmony, Configuration.WiggleFrequency != DefaultWiggleFrequency, typeof(WorldGenerator), nameof(WorldGenerator.WorldAngle), typeof(WorldAngle), nameof(WorldAngle.CalculateAngle), argumentTypes: [typeof(float), typeof(float)]);
    PatchAltBiomes(harmony, worldData);
    PatchNature(harmony);
    PatchLava(harmony);
    PatchTerrain(harmony);
    PatchWaterColor(harmony);
  }

  private static void PatchAltBiomes(Harmony harmony, bool worldData)
  {
    var shouldPatch = worldData && BiomeCalculator.HasAltBiomeData;
    Patches.Prefix(harmony, shouldPatch, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(AltBiomePlacement), nameof(AltBiomePlacement.PrepareWorldAltBiomes));
    Patches.Postfix(harmony, shouldPatch, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(AltBiomePlacement), nameof(AltBiomePlacement.ApplyWorldAltBiomes));
    Patches.Prefix(harmony, shouldPatch, typeof(BiomeSector), nameof(BiomeSector.CanAddModifier), typeof(AltBiomePlacement), nameof(AltBiomePlacement.BlockForcedSectors));
  }

  private static void PatchNature(Harmony harmony)
  {
    var shouldPatch = BiomeManager.HasData && BiomeManager.HasNatureOverrides;
    Patches.Prefix(harmony, shouldPatch, typeof(Beehive), nameof(Beehive.CheckBiome), typeof(BeehiveCheckBiome), nameof(BeehiveCheckBiome.BeginNatureBiomeCheck));
    Patches.Finalizer(harmony, shouldPatch, typeof(Beehive), nameof(Beehive.CheckBiome), typeof(BeehiveCheckBiome), nameof(BeehiveCheckBiome.EndNatureBiomeCheck));
    Patches.Prefix(harmony, shouldPatch, typeof(Player), nameof(Player.UpdatePlacementGhost), typeof(PlayerUpdatePlacementGhost), nameof(PlayerUpdatePlacementGhost.BeginNatureBiomeCheck));
    Patches.Finalizer(harmony, shouldPatch, typeof(Player), nameof(Player.UpdatePlacementGhost), typeof(PlayerUpdatePlacementGhost), nameof(PlayerUpdatePlacementGhost.EndNatureBiomeCheck));
    Patches.Prefix(harmony, shouldPatch, typeof(Plant), nameof(Plant.UpdateHealth), typeof(PlantUpdateHealth), nameof(PlantUpdateHealth.BeginNatureBiomeCheck));
    Patches.Finalizer(harmony, shouldPatch, typeof(Plant), nameof(Plant.UpdateHealth), typeof(PlantUpdateHealth), nameof(PlantUpdateHealth.EndNatureBiomeCheck));
    Patches.Prefix(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetGroundMaterial), typeof(HeightmapGetGroundMaterial), nameof(HeightmapGetGroundMaterial.BeginNatureBiomeCheck));
    Patches.Finalizer(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetGroundMaterial), typeof(HeightmapGetGroundMaterial), nameof(HeightmapGetGroundMaterial.EndNatureBiomeCheck));
    Patches.Postfix(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetBiome), typeof(GetBiomeHM), nameof(GetBiomeHM.ApplyNatureBiome));
    Patches.Postfix(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.FindBiome), typeof(HeightmapFindBiome), nameof(HeightmapFindBiome.ApplyNatureBiome));
  }

  private static void PatchLava(Harmony harmony)
  {
    var shouldPatch = BiomeManager.HasLavaOverrides;
    Patches.Transpiler(harmony, shouldPatch, typeof(Character), nameof(Character.UpdateLava), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLava));
    Patches.Prefix(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetLava), typeof(BiomeHeat), nameof(BiomeHeat.ReplaceGetLava));
    Patches.Prefix(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.IsLava), typeof(BiomeHeat), nameof(BiomeHeat.ReplaceIsLava));
    Patches.Transpiler(harmony, shouldPatch, typeof(Heightmap), nameof(Heightmap.GetHeightOffset), typeof(BiomeHeat), nameof(BiomeHeat.GetHeightOffset));
    Patches.Transpiler(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.ScanForLava), typeof(BiomeHeat), nameof(BiomeHeat.ScanForLava));
    Patches.Transpiler(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbient), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLavaAmbient));
    Patches.Transpiler(harmony, shouldPatch, typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbientLoops), typeof(BiomeHeat), nameof(BiomeHeat.UpdateLavaAmbientLoops));
    Patches.Transpiler(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.IsLavaPreHeightmap), typeof(BiomeHeat), nameof(BiomeHeat.IsLavaPreHeightmap));
  }

  private static void PatchTerrain(Harmony harmony)
  {
    var biomeData = BiomeManager.HasData;
    var territoryData = TerritoryManager.HasData;
    var anyData = biomeData || territoryData;
    Patches.Prefix(harmony, anyData, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(BiomeHeight), nameof(BiomeHeight.ReplaceTerrain));
    Patches.Postfix(harmony, anyData, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(BiomeHeight), nameof(BiomeHeight.ModifyHeight));
    Patches.Prefix(harmony, anyData, typeof(Minimap), nameof(Minimap.GetPixelColor), typeof(GetPixelColor), nameof(GetPixelColor.ApplyCustomColor));
    Patches.Prefix(harmony, biomeData, typeof(Minimap), nameof(Minimap.GetMaskColor), typeof(GetMaskColor), nameof(GetMaskColor.ApplyTerrain));
    Patches.Transpiler(harmony, biomeData, typeof(Minimap), nameof(Minimap.GenerateWorldMap), typeof(GenerateWorldMapHeight), nameof(GenerateWorldMapHeight.ApplyMapColorMultiplier));
    Patches.Prefix(harmony, biomeData, typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(HeightmapPatches), nameof(HeightmapPatches.GetBiomeColor), argumentTypes: [typeof(Heightmap.Biome)]);
    Patches.Prefix(harmony, territoryData, typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(HeightmapPatches), nameof(HeightmapPatches.CustomGetBiomeColor), argumentTypes: [typeof(float), typeof(float)]);
    Patches.Postfix(harmony, territoryData, typeof(Heightmap), nameof(Heightmap.OnDestroy), typeof(HeightmapPatches), nameof(HeightmapPatches.RemoveCornerTerritories), onUnpatch: HeightmapPatches.ClearCornerTerritories);
  }

  private static void PatchWaterColor(Harmony harmony)
  {
    var shouldPatch = Configuration.CustomWaterColor;
    Patches.Postfix(harmony, shouldPatch, typeof(Player), nameof(Player.AddKnownBiome), typeof(WaterColor), nameof(WaterColor.StartBiomeTransition), onUnpatch: WaterColor.StopTransition);
    Patches.Postfix(harmony, shouldPatch, typeof(Player), nameof(Player.OnSpawned), typeof(WaterColor), nameof(WaterColor.ResetTransition));
  }
}