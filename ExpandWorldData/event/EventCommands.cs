using ExpandWorldData;
using UnityEngine;

namespace ExpandWorld.Event;

public class Commands
{
  internal static void RunStartCommands(RandomEvent __instance)
  {
    if (Helper.IsClient()) return;
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.StartCommands != null)
      CommandManager.Run(data.StartCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }

  internal static void RunEndCommands(RandomEvent __instance)
  {
    if (Helper.IsClient()) return;
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.EndCommands != null)
      CommandManager.Run(data.EndCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }
}