using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;

namespace ExpandWorld.Spawn;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var enabled = SpawnManager.HasData || Event.EventManager.HasData;
    var active = enabled ? ActiveSpawns().ToHashSet() : [];
    PatchLifecycle(harmony);
    PatchData(harmony, active.Any(Loader.Data.ContainsKey));
    PatchObjects(harmony, active.Any(Loader.Objects.ContainsKey));
    PatchGlobalKeys(harmony, SpawnManager.Override?.Any(UsesNumericGlobalKey) == true);
  }

  private static IEnumerable<SpawnSystem.SpawnData> ActiveSpawns()
  {
    if (SpawnManager.Override != null)
      foreach (var spawn in SpawnManager.Override) yield return spawn;
    if (Event.EventManager.HasData && RandEventSystem.instance != null)
      foreach (var spawn in RandEventSystem.instance.m_events.SelectMany(entry => entry.m_spawn)) yield return spawn;
  }

  private static bool UsesNumericGlobalKey(SpawnSystem.SpawnData spawn)
  {
    if (string.IsNullOrWhiteSpace(spawn.m_requiredGlobalKey)) return false;
    var split = spawn.m_requiredGlobalKey.Trim().Split(' ');
    return split.Length > 1 && int.TryParse(split[1], out _);
  }

  private static void PatchLifecycle(Harmony harmony)
  {
    ExpandWorldData.Patches.Postfix(harmony, true, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(SpawnManager), nameof(SpawnManager.InitializeData), priority: Priority.VeryLow);
    ExpandWorldData.Patches.Postfix(harmony, true, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(SpawnManager), nameof(SpawnManager.InitializeSpawnSystem));
  }

  private static void PatchData(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.ApplyData));
  }

  private static void PatchObjects(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Postfix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.SpawnObjects));
  }

  private static void PatchGlobalKeys(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.Mutate));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.CheckRequirement), argumentTypes: [typeof(string)]);
    ExpandWorldData.Patches.Postfix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(GlobalKeys), nameof(GlobalKeys.Consume));
  }

}