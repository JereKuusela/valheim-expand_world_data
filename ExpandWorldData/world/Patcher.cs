using HarmonyLib;

namespace ExpandWorldData.World;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var environmentRules = BiomeManager.HasEnvironmentRules;
    Patches.Apply(harmony, environmentRules, typeof(EnvMan), nameof(EnvMan.GetAvailableEnvironments), typeof(GetAvailableEnvironments), nameof(GetAvailableEnvironments.FilterEnvironmentRules), HarmonyPatchType.Postfix);

    var globalKeyRules = BiomeManager.HasGlobalKeyRules;
    Patches.Apply(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.RPC_GlobalKeys), typeof(RPC_GlobalKeys), nameof(RPC_GlobalKeys.ResetEnvironmentPeriod), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.GlobalKeyAdd), typeof(GlobalKeyAdd), nameof(GlobalKeyAdd.ResetEnvironmentPeriod), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, globalKeyRules, typeof(ZoneSystem), nameof(ZoneSystem.GlobalKeyRemove), typeof(GlobalKeyRemove), nameof(GlobalKeyRemove.ResetEnvironmentPeriod), HarmonyPatchType.Postfix);

    var territoryData = TerritoryManager.HasData;
    Patches.Apply(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.PrepareTerritoryNameDisplay), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.AppendTerritoryNameToDisplay), HarmonyPatchType.Transpiler);
    Patches.Apply(harmony, territoryData, typeof(Minimap), nameof(Minimap.UpdateBiome), typeof(UpdateBiome), nameof(UpdateBiome.RestoreTerritoryNameDisplay), HarmonyPatchType.Postfix);
  }
}
