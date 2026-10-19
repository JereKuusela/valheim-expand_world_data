using HarmonyLib;
using ExpandWorldData.Biome;
using ExpandWorldData.Territory;

namespace ExpandWorldData.World;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var environmentRules = BiomeManager.HasEnvironmentRules;
    Patches.Postfix(harmony, environmentRules, typeof(EnvMan), nameof(EnvMan.GetAvailableEnvironments), typeof(GetAvailableEnvironments), nameof(GetAvailableEnvironments.FilterEnvironmentRules));

    var globalKeyRules = BiomeManager.HasGlobalKeyRules;
    Patches.Prefix(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.RPC_GlobalKeys), typeof(RPC_GlobalKeys), nameof(RPC_GlobalKeys.ResetEnvironmentPeriod));
    Patches.Postfix(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.GlobalKeyAdd), typeof(GlobalKeyAdd), nameof(GlobalKeyAdd.ResetEnvironmentPeriod));
    Patches.Postfix(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.GlobalKeyRemove), typeof(GlobalKeyRemove), nameof(GlobalKeyRemove.ResetEnvironmentPeriod));

    var territoryData = TerritoryManager.HasData;
    Patches.Prefix(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.PrepareTerritoryNameDisplay));
    Patches.Transpiler(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.AppendTerritoryNameToDisplay));
    Patches.Postfix(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.RestoreTerritoryNameDisplay));
  }
}
