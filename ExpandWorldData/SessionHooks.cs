using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace ExpandWorldData.SessionHooks;

[HarmonyPatch(typeof(ZNet), "Awake")]
internal static class Begin
{
  [HarmonyPriority(Priority.First)]
  static void Prefix(ZNet __instance) => Session.Begin(__instance);
}

[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
internal static class Connection
{
  [HarmonyPriority(Priority.First)]
  static void Prefix(ZNet __instance, ZNetPeer peer) => Session.Bind(__instance, peer);
}

[HarmonyPatch]
internal static class Shutdown
{
  static IEnumerable<MethodBase> TargetMethods()
  {
    yield return AccessTools.Method(typeof(ZNet), nameof(ZNet.Shutdown));
    yield return AccessTools.Method(typeof(ZNet), nameof(ZNet.ShutdownWithoutSave));
  }
  [HarmonyPriority(Priority.First)]
  static void Prefix(ZNet __instance) => Session.Quiesce(__instance);
  [HarmonyPriority(Priority.Last)]
  static void Postfix(ZNet __instance) => Session.End(__instance);
  static Exception? Finalizer(ZNet __instance, Exception? __exception)
  {
    if (__exception != null) Session.End(__instance, false);
    return __exception;
  }
}

[HarmonyPatch]
internal static class EnterWorld
{
  static IEnumerable<MethodBase> TargetMethods()
  {
    yield return AccessTools.Method(typeof(FejdStartup), "OnWorldStart");
    yield return AccessTools.Method(typeof(FejdStartup), "JoinServer");
  }
  static bool Prefix()
  {
    if (!Session.Failed) return true;
    UnifiedPopup.Push(new WarningPopup("Restart required", "Expand World Data could not finish resetting after the previous session. Restart Valheim before entering another world.", UnifiedPopup.Pop, false));
    return false;
  }
}
