using System;

namespace ExpandWorldData;

public static class ClientDataFlow
{
  public static void Apply(string yaml, bool enabled, Action<string> load, bool applyOnServer = false, bool ready = true)
  {
    if (!ready) return;
    if (Helper.IsClient())
    {
      if (enabled) load(yaml);
      return;
    }
    if (applyOnServer) load(yaml);
  }
}