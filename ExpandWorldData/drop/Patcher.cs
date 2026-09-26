using HarmonyLib;
using ExpandWorldData;

namespace ExpandWorld.Drops;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var shouldPatch = Configuration.DataDrops;
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(Loader), nameof(Loader.InitializeData), HarmonyPatchType.Postfix, Priority.VeryLow);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList), typeof(CharacterDropPatches), nameof(CharacterDropPatches.CharacterDropGenerateDropList), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(Piece), nameof(Piece.DropResources), typeof(PieceRequirementPatches), nameof(PieceRequirementPatches.PieceDropResources), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(Container), nameof(Container.AddDefaultItems), typeof(DropTablePatches), nameof(DropTablePatches.ContainerAddDefaultItems), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(FishingFloat), nameof(FishingFloat.Catch), typeof(DropTablePatches), nameof(DropTablePatches.FishingFloatCatch), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(Pickable), nameof(Pickable.RPC_Pick), typeof(DropTablePatches), nameof(DropTablePatches.PickableRPC_Pick), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(DropOnDestroyed), nameof(DropOnDestroyed.OnDestroyed), typeof(DropTablePatches), nameof(DropTablePatches.DropOnDestroyedOnDestroyed), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(LootSpawner), nameof(LootSpawner.UpdateSpawner), typeof(DropTablePatches), nameof(DropTablePatches.LootSpawnerUpdateSpawner), HarmonyPatchType.Transpiler);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(MineRock), nameof(MineRock.RPC_Hit), typeof(DropTablePatches), nameof(DropTablePatches.MineRockRPC_Hit), HarmonyPatchType.Transpiler);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(MineRock5), nameof(MineRock5.DamageArea), typeof(DropTablePatches), nameof(DropTablePatches.MineRock5DamageArea), HarmonyPatchType.Transpiler);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.RPC_Damage), typeof(DropTablePatches), nameof(DropTablePatches.TreeBaseRPC_Damage), HarmonyPatchType.Transpiler);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.SpawnLog), typeof(DropTablePatches), nameof(DropTablePatches.TreeBaseSpawnLog), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(TreeLog), nameof(TreeLog.Destroy), typeof(DropTablePatches), nameof(DropTablePatches.TreeLogDestroy), HarmonyPatchType.Prefix);
  }
}
