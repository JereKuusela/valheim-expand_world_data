using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ExpandWorldData.Data;
using HarmonyLib;
using UnityEngine;
using DataEntry = global::Data.DataEntry;

namespace ExpandWorldData.Drop;

// Applies custom object data to spawned drops, logs and stumps.
public static class DropSpawning
{
  // Set for the duration of a drop-spawning method (these run synchronously).
  private static Data? Current;

  internal static void SetContext(Component __instance) => Set(__instance.GetComponent<ZNetView>());
  internal static void SetFishContext(Fish fish) => Set(fish.m_nview);
  internal static void ClearContext() => Current = null;

  private static void Set(ZNetView view)
  {
    Current = DropManager.TryGetData(view, out var data) ? data : null;
  }

  // The ragdoll doesn't know the character, so the drop data is copied for the delayed loot spawn.
  internal static void RagdollSetup(Ragdoll __instance, CharacterDrop characterDrop)
  {
    if (!characterDrop) return;
    var target = __instance.GetComponent<ZNetView>()?.GetZDO();
    if (target == null) return;
    // Resolved data is copied because it may come from the prefab name fallback (no ews_drops on the ZDO).
    if (!DropManager.TryGetData(characterDrop.GetComponent<ZNetView>(), out var data)) return;
    target.Set(DropManager.HashDrop, data.name.GetStableHashCode());
  }

  private static bool IsActive(Vector3 pos) =>
    Current != null && DropManager.Matches(Current.biomes, Current.biomeAreas, pos);

  private static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, DataEntry? data)
  {
    if (data == null) return Object.Instantiate(prefab, pos, rot);
    return DataManager.Instantiate(prefab, pos, rot, data);
  }

  // Drop tables only return prefabs, so the entry is found by prefab (first one with data wins).
  internal static GameObject InstantiateDrop(GameObject prefab, Vector3 pos, Quaternion rot)
  {
    DataEntry? data = null;
    if (IsActive(pos))
    {
      foreach (var drop in Current!.drops)
      {
        if (drop.dataEntry == null || drop.obj != prefab) continue;
        if (!DropManager.Matches(drop.biomes, drop.biomeAreas, pos)) continue;
        data = drop.dataEntry;
        break;
      }
    }
    return Spawn(prefab, pos, rot, data);
  }

  internal static GameObject InstantiateLog(GameObject prefab, Vector3 pos, Quaternion rot) =>
    Spawn(prefab, pos, rot, IsActive(pos) ? Current!.logDataEntry : null);

  internal static GameObject InstantiateStump(GameObject prefab, Vector3 pos, Quaternion rot) =>
    Spawn(prefab, pos, rot, IsActive(pos) ? Current!.stumpDataEntry : null);

  private static readonly MethodInfo Instantiator = AccessTools.FirstMethod(typeof(Object), info =>
      info.Name == nameof(Object.Instantiate) && info.IsGenericMethodDefinition &&
      info.GetParameters().Length == 3 &&
      info.GetParameters()[1].ParameterType == typeof(Vector3) &&
      info.GetParameters()[2].ParameterType == typeof(Quaternion))
    .MakeGenericMethod(typeof(GameObject));

  private static readonly MethodInfo DropMethod = AccessTools.Method(typeof(DropSpawning), nameof(InstantiateDrop));
  private static readonly MethodInfo LogMethod = AccessTools.Method(typeof(DropSpawning), nameof(InstantiateLog));
  private static readonly MethodInfo StumpMethod = AccessTools.Method(typeof(DropSpawning), nameof(InstantiateStump));

  private static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions, params MethodInfo[] replacements)
  {
    var matcher = new CodeMatcher(instructions);
    var match = new CodeMatch(OpCodes.Call, Instantiator);
    for (var i = 0; ; i++)
    {
      matcher.MatchForward(false, match);
      if (matcher.IsInvalid) break;
      // Extra calls beyond the given replacements reuse the last one.
      matcher.Set(OpCodes.Call, replacements[System.Math.Min(i, replacements.Length - 1)]).Advance(1);
    }
    return matcher.InstructionEnumeration();
  }

  internal static IEnumerable<CodeInstruction> ReplaceAll(IEnumerable<CodeInstruction> instructions) =>
    Replace(instructions, DropMethod);

  // Log first, then stump.
  internal static IEnumerable<CodeInstruction> TreeBaseSpawnLog(IEnumerable<CodeInstruction> instructions) =>
    Replace(instructions, LogMethod, StumpMethod);

  // Drops first, then sub logs.
  internal static IEnumerable<CodeInstruction> TreeLogDestroy(IEnumerable<CodeInstruction> instructions) =>
    Replace(instructions, DropMethod, LogMethod);
}
