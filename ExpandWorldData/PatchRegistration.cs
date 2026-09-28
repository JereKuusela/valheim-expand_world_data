using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Service;

namespace ExpandWorldData;

public static class Patches
{
  // Value is the state the patch was applied with, so state-dependent transpilers can be re-applied.
  private static readonly Dictionary<(MethodBase Original, MethodInfo Patch), object?> Registered = [];

  public static void Apply(
    Harmony harmony,
    bool shouldPatch,
    Type originalType,
    string originalName,
    Type patchType,
    string patchName,
    HarmonyPatchType patchKind,
    int priority = Priority.Normal,
    Type[]? argumentTypes = null,
    bool enumerator = false,
    object? state = null,
    Action? onUnpatch = null)
  {
    try
    {
      var original = ResolveOriginal(originalType, originalName, argumentTypes, enumerator);
      var patch = AccessTools.Method(patchType, patchName)
        ?? throw new MissingMethodException(patchType.FullName, patchName);
      var key = (original, patch);
      var registered = Registered.TryGetValue(key, out var currentState);
      if (registered && shouldPatch && Equals(currentState, state)) return;
      if (registered)
      {
        harmony.Unpatch(original, patch);
        Registered.Remove(key);
        if (!shouldPatch) onUnpatch?.Invoke();
      }
      if (!shouldPatch) return;
      Patch(harmony, original, patch, patchKind, priority);
      Registered[key] = state;
    }
    catch (Exception e)
    {
      Log.Error($"Failed to {(shouldPatch ? "patch" : "unpatch")} {originalType.Name}.{originalName} with {patchType.Name}.{patchName}: {e.Message}");
    }
  }

  private static MethodBase ResolveOriginal(Type originalType, string originalName, Type[]? argumentTypes, bool enumerator)
  {
    var method = AccessTools.Method(originalType, originalName, argumentTypes)
      ?? throw new MissingMethodException(originalType.FullName, originalName);
    if (!enumerator) return method;
    return AccessTools.EnumeratorMoveNext(method)
      ?? throw new MissingMethodException(originalType.FullName, $"{originalName}.MoveNext");
  }

  private static void Patch(Harmony harmony, MethodBase original, MethodInfo patch, HarmonyPatchType patchKind, int priority)
  {
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
  }
}