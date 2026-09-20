using ExpandWorldData;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorld.Event;

[HarmonyPatch(typeof(RandomEvent))]
public class Commands
{
  [HarmonyPatch(nameof(RandomEvent.OnStart)), HarmonyPostfix]
  static void OnStart(RandomEvent __instance)
  {
    if (!Configuration.DataEvents || Helper.IsClient()) return;
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.StartCommands != null)
      CommandManager.Run(data.StartCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }

  [HarmonyPatch(nameof(RandomEvent.OnStop)), HarmonyPostfix]
  static void OnStop(RandomEvent __instance)
  {
    if (!Configuration.DataEvents || Helper.IsClient()) return;
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.EndCommands != null)
      CommandManager.Run(data.EndCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }
}