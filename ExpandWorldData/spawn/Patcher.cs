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
    var enabled = Configuration.DataSpawns || Configuration.DataEvents;
    var active = enabled ? ActiveSpawns().ToHashSet() : [];
    PatchLifecycle(harmony, Configuration.DataSpawns);
    PatchData(harmony, enabled && active.Any(Loader.Data.ContainsKey));
    PatchObjects(harmony, enabled && active.Any(Loader.Objects.ContainsKey));
    PatchGlobalKeys(harmony, Configuration.DataSpawns && Manager.Override?.Any(UsesNumericGlobalKey) == true);
  }

  private static IEnumerable<SpawnSystem.SpawnData> ActiveSpawns()
  {
    if (Configuration.DataSpawns && Manager.Override != null)
      foreach (var spawn in Manager.Override) yield return spawn;
    if (Configuration.DataEvents && RandEventSystem.instance != null)
      foreach (var spawn in RandEventSystem.instance.m_events.SelectMany(entry => entry.m_spawn)) yield return spawn;
  }

  private static bool UsesNumericGlobalKey(SpawnSystem.SpawnData spawn)
  {
    if (string.IsNullOrWhiteSpace(spawn.m_requiredGlobalKey)) return false;
    var split = spawn.m_requiredGlobalKey.Trim().Split(' ');
    return split.Length > 1 && int.TryParse(split[1], out _);
  }

  private static void PatchLifecycle(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(Manager), nameof(Manager.InitializeData), HarmonyPatchType.Postfix, Priority.VeryLow);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(Manager), nameof(Manager.InitializeSpawnSystem), HarmonyPatchType.Postfix);
  }

  private static void PatchData(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.ApplyData), HarmonyPatchType.Prefix);
  }

  private static void PatchObjects(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.SpawnObjects), HarmonyPatchType.Postfix);
  }

  private static void PatchGlobalKeys(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.Mutate), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.CheckRequirement), HarmonyPatchType.Prefix, argumentTypes: [typeof(string)]);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(GlobalKeys), nameof(GlobalKeys.Consume), HarmonyPatchType.Postfix);
  }

}