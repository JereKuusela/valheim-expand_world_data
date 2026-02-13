using System;
using BepInEx;
using Data;
using HarmonyLib;
using Service;
using UnityEngine;
namespace ExpandWorldData;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInIncompatibility("expand_world")]
public class EWD : BaseUnityPlugin
{
  public const string GUID = "expand_world_data";
  public const string NAME = "Expand World Data";
  public const string VERSION = "1.58";
#nullable disable
  public static EWD Instance;
  public static Harmony Harmony;
#nullable enable
  public static ServerSync.ConfigSync ConfigSync = new(GUID, true)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    ModRequired = true,
    IsLocked = true
  };
  public void InvokeRegenerate()
  {
    // Nothing to regenerate because the world hasn't been generated yet.
    if (WorldGenerator.instance?.m_world?.m_menu != false) return;
    // Debounced for smooth config editing.
    CancelInvoke("Regenerate");
    Invoke("Regenerate", 1.0f);
  }
  public void Regenerate() => WorldInfo.AutomaticRegenerate(Harmony);
  public void Awake()
  {
    Instance = this;
    Log.Init(Logger);
    Yaml.Init();
    BiomeManager.SetupBiomeArrays();
    ConfigWrapper wrapper = new("expand_config", Config, ConfigSync, InvokeRegenerate);
    Configuration.Init(wrapper);
    Harmony = new(GUID);
    Harmony.PatchAll();
    try
    {
      if (!System.IO.Directory.Exists(Yaml.BaseDirectory))
        System.IO.Directory.CreateDirectory(Yaml.BaseDirectory);
      if (Configuration.DataReload)
      {
        Yaml.SetupWatcher(Config);
        DataLoading.SetupWatcher();
        BiomeManager.SetupWatcher();
        LocationLoading.SetupWatcher();
        VegetationLoading.SetupWatcher();
        WorldManager.SetupWatcher();
        ClutterManager.SetupWatcher();
        EnvironmentManager.SetupWatcher();
        Dungeon.Loader.SetupWatcher();
        RoomLoading.SetupWatcher();
        BlueprintManager.SetupBlueprintWatcher();
      }
    }
    catch (Exception e)
    {
      Log.Error(e.StackTrace);
    }
  }
  public void Start()
  {
    BiomeManager.NamesFromFile();
    new DebugCommands();
  }
  public void LateUpdate()
  {
    WaterColor.Transition(Time.deltaTime);
  }

#pragma warning disable IDE0051
  private void OnDestroy()
  {
    Config.Save();
  }
#pragma warning restore IDE0051


}

