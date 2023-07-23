using BepInEx.Bootstrap;
namespace ExpandWorldData;

public class CustomRaids
{
  public const string GUID = "asharppen.valheim.custom_raids";
  public static void Run()
  {
    if (!Chainloader.PluginInfos.ContainsKey(GUID)) return;
    EWD.Log.LogInfo("\"Custom Raids\" detected. Disabling event data.");
    Configuration.configDataEvents.Value = false;
  }
}
