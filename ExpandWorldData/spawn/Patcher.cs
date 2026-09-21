using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;

namespace ExpandWorld.Spawn;

public static class Patcher
{
  private static bool LifecyclePatched;
  private static bool DataPatched;
  private static bool ObjectsPatched;
  private static bool GlobalKeysPatched;

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
    if (shouldPatch == LifecyclePatched) return;
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(Manager), nameof(Manager.InitializeData), HarmonyPatchType.Postfix, Priority.VeryLow);
    SetPatch(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(Manager), nameof(Manager.InitializeSpawnSystem), HarmonyPatchType.Postfix);
    LifecyclePatched = shouldPatch;
  }

  private static void PatchData(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == DataPatched) return;
    SetPatch(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.ApplyData), HarmonyPatchType.Prefix);
    DataPatched = shouldPatch;
  }

  private static void PatchObjects(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == ObjectsPatched) return;
    SetPatch(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.SpawnObjects), HarmonyPatchType.Postfix);
    ObjectsPatched = shouldPatch;
  }

  private static void PatchGlobalKeys(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == GlobalKeysPatched) return;
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.Mutate), HarmonyPatchType.Prefix);
    SetPatch(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.CheckRequirement), HarmonyPatchType.Prefix, argumentTypes: [typeof(string)]);
    SetPatch(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(GlobalKeys), nameof(GlobalKeys.Consume), HarmonyPatchType.Postfix);
    GlobalKeysPatched = shouldPatch;
  }

  private static void SetPatch(Harmony harmony, bool shouldPatch, Type originalType, string originalName, Type patchType, string patchName, HarmonyPatchType type, int priority = Priority.Normal, Type[]? argumentTypes = null)
  {
    var original = AccessTools.Method(originalType, originalName, argumentTypes);
    var patch = AccessTools.Method(patchType, patchName);
    if (shouldPatch)
    {
      var harmonyPatch = new HarmonyMethod(patch) { priority = priority };
      if (type == HarmonyPatchType.Prefix) harmony.Patch(original, prefix: harmonyPatch);
      else harmony.Patch(original, postfix: harmonyPatch);
    }
    else harmony.Unpatch(original, patch);
  }
}