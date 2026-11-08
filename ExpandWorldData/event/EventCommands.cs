using Service;
using UnityEngine;

namespace ExpandWorldData.Event;

public class Commands
{
  internal static void RunStartCommands(RandomEvent __instance)
  {
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.StartCommands != null)
      CommandManager.RunCommand(data.StartCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }

  internal static void RunEndCommands(RandomEvent __instance)
  {
    var baseEvent = RandEventSystem.instance.GetEvent(__instance.m_name);
    if (Loader.ExtraData.TryGetValue(baseEvent, out var data) && data.EndCommands != null)
      CommandManager.RunCommand(data.EndCommands, __instance.m_pos, Quaternion.identity.eulerAngles);
  }
}
