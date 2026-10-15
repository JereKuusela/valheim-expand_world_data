using HarmonyLib;

namespace ExpandWorld.Drops;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    ExpandWorldData.Patches.Postfix(harmony, true, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(DropManager), nameof(DropManager.InitializeData), priority: Priority.VeryLow);
    var shouldPatch = DropManager.HasData;
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList), typeof(CharacterDropPatches), nameof(CharacterDropPatches.CharacterDropGenerateDropList));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(Piece), nameof(Piece.DropResources), typeof(PieceRequirementPatches), nameof(PieceRequirementPatches.PieceDropResources));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(Container), nameof(Container.AddDefaultItems), typeof(DropTablePatches), nameof(DropTablePatches.ContainerAddDefaultItems));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(FishingFloat), nameof(FishingFloat.Catch), typeof(DropTablePatches), nameof(DropTablePatches.FishingFloatCatch));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(Pickable), nameof(Pickable.RPC_Pick), typeof(DropTablePatches), nameof(DropTablePatches.PickableRPC_Pick));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(DropOnDestroyed), nameof(DropOnDestroyed.OnDestroyed), typeof(DropTablePatches), nameof(DropTablePatches.DropOnDestroyedOnDestroyed));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(LootSpawner), nameof(LootSpawner.UpdateSpawner), typeof(DropTablePatches), nameof(DropTablePatches.LootSpawnerUpdateSpawner));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(MineRock), nameof(MineRock.RPC_Hit), typeof(DropTablePatches), nameof(DropTablePatches.MineRockRPC_Hit));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(MineRock5), nameof(MineRock5.DamageArea), typeof(DropTablePatches), nameof(DropTablePatches.MineRock5DamageArea));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.RPC_Damage), typeof(DropTablePatches), nameof(DropTablePatches.TreeBaseRPC_Damage));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.SpawnLog), typeof(DropTablePatches), nameof(DropTablePatches.TreeBaseSpawnLog));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(TreeLog), nameof(TreeLog.Destroy), typeof(DropTablePatches), nameof(DropTablePatches.TreeLogDestroy));
    PatchSpawning(harmony, DropManager.HasCustomData);
  }

  // Custom object data for spawned drops: context is set around each method, instantiate calls are replaced.
  private static void PatchSpawning(Harmony harmony, bool shouldPatch)
  {
    Context(harmony, shouldPatch, typeof(CharacterDrop), nameof(CharacterDrop.OnDeath));
    Context(harmony, shouldPatch, typeof(Ragdoll), nameof(Ragdoll.SpawnLoot));
    Context(harmony, shouldPatch, typeof(Piece), nameof(Piece.DropResources));
    Context(harmony, shouldPatch, typeof(Pickable), nameof(Pickable.RPC_Pick));
    Context(harmony, shouldPatch, typeof(DropOnDestroyed), nameof(DropOnDestroyed.OnDestroyed));
    Context(harmony, shouldPatch, typeof(LootSpawner), nameof(LootSpawner.UpdateSpawner));
    Context(harmony, shouldPatch, typeof(MineRock), nameof(MineRock.RPC_Hit));
    Context(harmony, shouldPatch, typeof(MineRock5), nameof(MineRock5.DamageArea));
    Context(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.RPC_Damage));
    Context(harmony, shouldPatch, typeof(TreeLog), nameof(TreeLog.Destroy));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(FishingFloat), nameof(FishingFloat.Catch), typeof(DropSpawning), nameof(DropSpawning.SetFishContext));
    ExpandWorldData.Patches.Finalizer(harmony, shouldPatch, typeof(FishingFloat), nameof(FishingFloat.Catch), typeof(DropSpawning), nameof(DropSpawning.ClearContext));
    ExpandWorldData.Patches.Postfix(harmony, shouldPatch, typeof(Ragdoll), nameof(Ragdoll.Setup), typeof(DropSpawning), nameof(DropSpawning.RagdollSetup));

    Replace(harmony, shouldPatch, typeof(CharacterDrop), nameof(CharacterDrop.DropItems));
    Replace(harmony, shouldPatch, typeof(Piece), nameof(Piece.DropResources));
    Replace(harmony, shouldPatch, typeof(FishingFloat), nameof(FishingFloat.Catch));
    Replace(harmony, shouldPatch, typeof(Pickable), nameof(Pickable.Drop));
    Replace(harmony, shouldPatch, typeof(DropOnDestroyed), nameof(DropOnDestroyed.OnDestroyed));
    Replace(harmony, shouldPatch, typeof(LootSpawner), nameof(LootSpawner.UpdateSpawner));
    Replace(harmony, shouldPatch, typeof(MineRock), nameof(MineRock.RPC_Hit));
    Replace(harmony, shouldPatch, typeof(MineRock5), nameof(MineRock5.DamageArea));
    Replace(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.RPC_Damage));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(TreeBase), nameof(TreeBase.SpawnLog), typeof(DropSpawning), nameof(DropSpawning.TreeBaseSpawnLog));
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, typeof(TreeLog), nameof(TreeLog.Destroy), typeof(DropSpawning), nameof(DropSpawning.TreeLogDestroy));
  }

  private static void Context(Harmony harmony, bool shouldPatch, System.Type type, string method)
  {
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, type, method, typeof(DropSpawning), nameof(DropSpawning.SetContext));
    ExpandWorldData.Patches.Finalizer(harmony, shouldPatch, type, method, typeof(DropSpawning), nameof(DropSpawning.ClearContext));
  }

  private static void Replace(Harmony harmony, bool shouldPatch, System.Type type, string method) =>
    ExpandWorldData.Patches.Transpiler(harmony, shouldPatch, type, method, typeof(DropSpawning), nameof(DropSpawning.ReplaceAll));
}