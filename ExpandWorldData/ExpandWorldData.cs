using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Service;
namespace ExpandWorldData;
[BepInPlugin(GUID, NAME, VERSION)]
[BepInIncompatibility("expand_world")]
public class EWD : BaseUnityPlugin
{
  public const string GUID = "expand_world_data";
  public const string NAME = "Expand World Data";
  public const string VERSION = "1.19";
#nullable disable
  public static ManualLogSource Log;
  public static EWD Instance;
#nullable enable
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    ModRequired = true,
    IsLocked = true
  };
  public static bool NeedsMigration = File.Exists(Path.Combine(Paths.ConfigPath, "expand_world.cfg")) && !File.Exists(Path.Combine(Paths.ConfigPath, "expand_world_data.cfg"));
  public static string YamlDirectory = Path.Combine(Paths.ConfigPath, "expand_world");
  public static string BackupDirectory = Path.Combine(Paths.ConfigPath, "expand_world_backups");
  public void InvokeRegenerate()
  {
    // Nothing to regenerate because the world hasn't been generated yet.
    if (WorldGenerator.instance?.m_world?.m_menu != false) return;
    // Debounced for smooth config editing.
    CancelInvoke("Regenerate");
    Invoke("Regenerate", 1.0f);
  }
  public void Regenerate() => WorldInfo.AutomaticRegenerate();
  public void Awake()
  {
    Instance = this;
    Log = Logger;
    BiomeManager.SetupBiomeArrays();
    // Migrating would be pointless if yaml get reset.
    if (!NeedsMigration)
      YamlCleanUp();
    if (!Directory.Exists(YamlDirectory))
      Directory.CreateDirectory(YamlDirectory);
    ConfigWrapper wrapper = new("expand_config", Config, ConfigSync, InvokeRegenerate);
    Configuration.Init(wrapper);
    if (NeedsMigration)
      MigrateOldConfig();
    Harmony harmony = new(GUID);
    harmony.PatchAll();
    try
    {
      if (Configuration.DataReload)
      {
        DataManager.SetupWatcher(Config);
        DataLoading.SetupWatcher();
        BiomeManager.SetupWatcher();
        LocationLoading.SetupWatcher();
        VegetationLoading.SetupWatcher();
        WorldManager.SetupWatcher();
        ClutterManager.SetupWatcher();
        EnvironmentManager.SetupWatcher();
        Dungeon.Loader.SetupWatcher();
        RoomLoading.SetupWatcher();
        DataManager.SetupBlueprintWatcher();
      }
    }
    catch (Exception e)
    {
      Log.LogError(e);
    }
  }
  public void Start()
  {
    BiomeManager.NamesFromFile();
  }
  private void MigrateOldConfig()
  {
    Log.LogWarning("Migrating old config file and enabling Legacy Generation.");
    Configuration.configLegacyGeneration.Value = true;
    Config.Save();
    var from = File.ReadAllLines(Path.Combine(Paths.ConfigPath, "expand_world.cfg"));
    var to = File.ReadAllLines(Config.ConfigFilePath);
    foreach (var line in from)
    {
      var split = line.Split('=');
      if (split.Length != 2) continue;
      for (var i = 0; i < to.Length; ++i)
      {
        if (to[i].StartsWith(split[0]))
          to[i] = line;
      }
    }
    File.WriteAllLines(Config.ConfigFilePath, to);
    Config.Reload();
  }
#pragma warning disable IDE0051
  private void OnDestroy()
  {
    Config.Save();
  }
#pragma warning restore IDE0051

  private void YamlCleanUp()
  {
    try
    {
      if (!Directory.Exists(YamlDirectory)) return;
      if (File.Exists(Config.ConfigFilePath)) return;
      Directory.Delete(YamlDirectory, true);
    }
    catch
    {
      Log.LogWarning("Failed to remove old yaml files.");
    }
  }
}

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    new DebugCommands();
  }
}
