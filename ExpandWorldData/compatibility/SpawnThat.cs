using BepInEx.Bootstrap;
namespace ExpandWorldData;

public class SpawnThat
{
  public const string GUID = "asharppen.valheim.spawn_that";
  public static void Run()
  {
    if (!Chainloader.PluginInfos.ContainsKey(GUID)) return;
    EWD.Log.LogInfo("\"Spawn That\" detected. Disabling spawns data.");
    Configuration.configDataSpawns.Value = false;
  }
}
