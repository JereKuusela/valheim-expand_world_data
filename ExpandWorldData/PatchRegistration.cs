using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ExpandWorldData;

public static class Patches
{
  private static readonly HashSet<(Type PatchType, string PatchName)> Registered = [];

  public static bool IsRegistered(Type patchType, string patchName) => Registered.Contains((patchType, patchName));

  public static void Apply(
    Harmony harmony,
    bool shouldPatch,
    Type originalType,
    string originalName,
    Type patchType,
    string patchName,
    HarmonyPatchType patchKind,
    int priority = Priority.Normal,
    Type[]? argumentTypes = null)
  {
    var original = AccessTools.Method(originalType, originalName, argumentTypes)
      ?? throw new MissingMethodException(originalType.FullName, originalName);
    var patch = AccessTools.Method(patchType, patchName)
      ?? throw new MissingMethodException(patchType.FullName, patchName);
    var key = (patchType, patchName);

    if (!shouldPatch)
    {
      if (!Registered.Contains(key)) return;
      harmony.Unpatch(original, patch);
      Registered.Remove(key);
      return;
    }
    if (Registered.Contains(key)) return;

    var harmonyMethod = new HarmonyMethod(patch) { priority = priority };
    switch (patchKind)
    {
      case HarmonyPatchType.Prefix:
        harmony.Patch(original, prefix: harmonyMethod);
        break;
      case HarmonyPatchType.Postfix:
        harmony.Patch(original, postfix: harmonyMethod);
        break;
      case HarmonyPatchType.Transpiler:
        harmony.Patch(original, transpiler: harmonyMethod);
        break;
      case HarmonyPatchType.Finalizer:
        harmony.Patch(original, finalizer: harmonyMethod);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(patchKind), patchKind, "Unsupported Harmony patch kind.");
    }
    Registered.Add(key);
  }
}