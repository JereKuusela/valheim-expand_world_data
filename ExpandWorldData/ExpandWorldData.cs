using System;
using BepInEx;
using BepInEx.Bootstrap;
using Data;
using HarmonyLib;
using Service;
using Common;
using UnityEngine;
using ExpandWorldData.Biome;
using ExpandWorldData.Clutter;
using ExpandWorldData.Features;
using ExpandWorldData.Dungeon;
using ExpandWorldData.Environment;
using ExpandWorldData.Territory;
using ExpandWorldData.World;
using ExpandWorldData.Data;
using ExpandWorldData.Locations;
using ExpandWorldData.AltBiomes;
using ExpandWorldData.Vegetation;
using ExpandWorldData.Migration;
namespace ExpandWorldData;

[BepInPlugin(GUID, NAME, VERSION)]
public class EWD : BaseUnityPlugin
{
  public const string GUID = "expand_world_data";
  public const string NAME = "Expand World Data";
  public const string VERSION = "1.78.5";
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
  public void Awake()
  {
    Instance = this;
    Log.Init(Logger);
    Yaml.Init();
    Refresh.Register(Regen.Patches, () => Patcher.Update(Harmony));
    Refresh.Register(Regen.Water, WaterColor.Regenerate);
    var initialRevision = Migrations.ReadRevision(Config.ConfigFilePath);
    ConfigWrapper wrapper = new("expand_config", Config, ConfigSync, () => Refresh.Request(Regen.World));
    Configuration.Init(wrapper, initialRevision);
    LegacyEventsConfiguration.Migrate(Config);
    Harmony = new(GUID);
    Patcher.Initialize(Harmony);
    try
    {
      if (!System.IO.Directory.Exists(Yaml.BaseDirectory))
        System.IO.Directory.CreateDirectory(Yaml.BaseDirectory);
      if (Configuration.DataReload)
      {
        Yaml.SetupWatcher(Config);
        DataLoading.SetupWatcher();
        BlueprintManager.SetupBlueprintWatcher();
      }
      BiomeManager.SetupWatcher();
      TerritoryManager.SetupWatcher();
      LocationManager.SetupWatcher();
      VegetationManager.SetupWatcher();
      WorldManager.SetupWatcher();
      ClutterManager.SetupWatcher();
      EnvironmentManager.SetupWatcher();
      AltBiomeManager.SetupWatcher();
      DungeonManager.SetupWatcher();
      RoomManager.SetupWatcher();
      Spawn.SpawnManager.SetupWatcher();
      Event.EventManager.SetupWatcher();
      Drop.DropManager.SetupWatcher();
    }
    catch (Exception e)
    {
      Log.Error(e.StackTrace);
    }
  }
  public void Start()
  {
    if (Chainloader.PluginInfos.ContainsKey("expand_world_events") && !Configuration.DataEvents)
    {
      Configuration.configDataEvents.Value = true;
    }
    if (Chainloader.PluginInfos.ContainsKey("expand_world_spawns"))
    {
      if (!Configuration.DataSpawns)
      {
        Configuration.configDataSpawns.Value = true;
      }
      Configuration.configDataDrops.Value = true;
    }
    BiomeManager.NamesFromFile();
    new DebugCommands();
  }
  public void LateUpdate()
  {
    if (Yaml.WorldLoading && Yaml.WorldLoadedAt != DateTime.MaxValue)
    {
      Yaml.WorldLoadedAt = DateTime.UtcNow;
      Yaml.WorldLoading = false;
    }
    Refresh.Tick(Time.deltaTime);
    WaterColor.Transition(Time.deltaTime);
  }

#pragma warning disable IDE0051
  private void OnDestroy()
  {
    Config.Save();
  }
#pragma warning restore IDE0051


}

