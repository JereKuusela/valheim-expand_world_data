using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using Service;

namespace ExpandWorldData;
public partial class Configuration
{
#nullable disable
  public static ConfigEntry<bool> configServerOnly;
  public static bool ServerOnly => configServerOnly.Value;
  public static ConfigEntry<bool> configSplitDataPerMod;
  public static bool SplitDataPerMod => configSplitDataPerMod.Value;
  public static ConfigEntry<bool> configLegacyGeneration;
  public static bool LegacyGeneration => configLegacyGeneration.Value;
  public static ConfigEntry<bool> configRegenerateMap;
  public static bool RegenerateMap => configRegenerateMap.Value;

  public static ConfigEntry<bool> configZoneSpawners;
  public static bool ZoneSpawners => configZoneSpawners.Value;

  public static ConfigEntry<string> configDistanceWiggleLength;
  public static float DistanceWiggleLength => ConfigWrapper.Floats[configDistanceWiggleLength];
  public static ConfigEntry<string> configDistanceWiggleWidth;
  public static float DistanceWiggleWidth => ConfigWrapper.Floats[configDistanceWiggleWidth];
  public static ConfigEntry<string> configWiggleFrequency;
  public static float WiggleFrequency => ConfigWrapper.Floats[configWiggleFrequency];
  public static ConfigEntry<string> configWiggleWidth;
  public static float WiggleWidth => ConfigWrapper.Floats[configWiggleWidth];
  public static ConfigEntry<string> configAshlandsWidthRestriction;
  //public static double AshlandsWidthRestriction => RestrictAshlands ? ConfigWrapper.Floats[configAshlandsWidthRestriction] : double.PositiveInfinity;
  public static double AshlandsWidthRestriction => RestrictAshlands ? 7500f : double.PositiveInfinity;
  //public static double AshlandsDepthRestriction => RestrictAshlands ? ConfigWrapper.Floats[configAshlandsDepthRestriction] : double.PositiveInfinity;
  public static double AshlandsDepthRestriction => RestrictAshlands ? 600f : double.PositiveInfinity;
  public static ConfigEntry<string> configAshlandsDistanceRestriction;
  //public static double AshlandsDistanceRestriction => RestrictAshlands ? ConfigWrapper.Floats[configAshlandsDistanceRestriction] : double.PositiveInfinity;
  public static double AshlandsDistanceRestriction => RestrictAshlands ? 1000f : double.PositiveInfinity;

  public static ConfigEntry<string> configAshlandsDepthRestriction;
  public static ConfigEntry<bool> configRestrictAshlands;
  public static bool RestrictAshlands => configRestrictAshlands.Value;
  public static ConfigEntry<bool> configAshlandsGap;
  public static bool AshlandsGap => configAshlandsGap.Value;
  public static ConfigEntry<bool> configDeepNorthGap;
  public static bool DeepNorthGap => configDeepNorthGap.Value;


  public static CustomSyncedValue<string> valueBiomeData;
  public static CustomSyncedValue<string> valueWorldData;
  public static CustomSyncedValue<string> valueClutterData;
  public static CustomSyncedValue<string> valueEnvironmentData;
  public static CustomSyncedValue<string> valueNoBuildData;
  public static ConfigEntry<bool> configDataEnvironments;
  public static bool DataEnvironments => configDataEnvironments.Value;
  public static ConfigEntry<bool> configDataVegetation;
  public static bool DataVegetation => configDataVegetation.Value;
  public static ConfigEntry<bool> configDataClutter;
  public static bool DataClutter => configDataClutter.Value;
  public static ConfigEntry<bool> configDataDungeons;
  public static bool DataDungeons => configDataDungeons.Value;
  public static ConfigEntry<bool> configDataRooms;
  public static bool DataRooms => configDataRooms.Value;

  public static ConfigEntry<bool> configDataLocation;
  public static bool DataLocation => configDataLocation.Value;
  public static ConfigEntry<bool> configDataBiome;
  public static bool DataBiome => configDataBiome.Value;
  public static ConfigEntry<bool> configDataWorld;
  public static bool DataWorld => configDataWorld.Value;
  public static ConfigEntry<bool> configDataMigration;
  public static bool DataMigration => configDataMigration.Value;
  public static ConfigEntry<bool> configDataReload;
  public static bool DataReload => configDataReload.Value;

  public static ConfigEntry<string> configBlueprintFolder;
  public static string BlueprintGlobalFolder => Path.Combine("BepInEx", "config", configBlueprintFolder.Value);
  public static string BlueprintLocalFolder => Path.Combine(Paths.ConfigPath, configBlueprintFolder.Value);
#nullable enable

  public static void HandleRestrictAshlandsPosition()
  {
    if (WorldGenerator.instance == null) return;
    if (GetAshlandsHeight.Patch(EWD.Harmony, AshlandsWidthRestriction, AshlandsDepthRestriction, AshlandsDistanceRestriction))
      EWD.Instance.InvokeRegenerate();
  }
  public static void HandleAshlandsGap()
  {
    if (WorldGenerator.instance == null) return;
    if (CreateAshlandsGap.Patch(EWD.Harmony, !AshlandsGap))
      EWD.Instance.InvokeRegenerate();
  }
  public static void HandleDeepNorthGap()
  {
    if (WorldGenerator.instance == null) return;
    if (CreateDeepNorthGap.Patch(EWD.Harmony, !DeepNorthGap))
      EWD.Instance.InvokeRegenerate();
  }
  public static void Init(ConfigWrapper wrapper)
  {
    var section = "1. General";
    configRegenerateMap = wrapper.Bind(section, "Regenerate map", true, false, "If true, the world map is regenerated automatically on data changes.");
    configServerOnly = wrapper.Bind(section, "Server only", false, false, "If true, enables server side only mode and clients can't have the mod installed.");
    configLegacyGeneration = wrapper.Bind(section, "Legacy generation", false, true, "Old Expand World had a bug that cause incorrect generation near biome borders. Set this true for older worlds.");
    configZoneSpawners = wrapper.Bind(section, "Zone spawners", true, false, "If disabled, zone spawners are not generated.");

    section = "2. Features";

    configDistanceWiggleLength = wrapper.BindFloat(section, "Distance wiggle length", 500f, false);
    configDistanceWiggleLength.SettingChanged += (s, e) => WorldManager.FromFile();
    configDistanceWiggleWidth = wrapper.BindFloat(section, "Distance wiggle width", 0.01f, false);
    configDistanceWiggleWidth.SettingChanged += (s, e) => WorldManager.FromFile();
    configWiggleFrequency = wrapper.BindFloat(section, "Wiggle frequency", 20f, false, "How many wiggles are per each circle.");
    configWiggleFrequency.SettingChanged += (s, e) => WorldManager.FromFile();
    configWiggleWidth = wrapper.BindFloat(section, "Wiggle width", 100f, false, "How many meters are the wiggles.");
    configWiggleWidth.SettingChanged += (s, e) => WorldManager.FromFile();

    section = "3. Data";
    configDataReload = wrapper.Bind(section, "Automatic data reload", true, false, "Data is loaded automatically on file changes. Requires restart to take effect.");
    configSplitDataPerMod = wrapper.Bind(section, "Split data per mod", true, false, "If true, created data is saved to multiple files. If false, all data is saved in one file.");
    configDataMigration = wrapper.Bind(section, "Automatic data migration", true, false, "Automatically add missing location, rooms and vegetation entries.");
    configDataEnvironments = wrapper.Bind(section, "Environment data", true, false, "Use environment data");
    configDataEnvironments.SettingChanged += (s, e) => EnvironmentManager.FromSetting(valueEnvironmentData.Value);
    configDataBiome = wrapper.Bind(section, "Biome data", true, true, "Use biome data");
    configDataBiome.SettingChanged += (s, e) => BiomeManager.FromSetting(valueBiomeData.Value);
    configDataClutter = wrapper.Bind(section, "Clutter data", true, false, "Use clutter data");
    configDataClutter.SettingChanged += (s, e) => ClutterManager.FromFile();
    configDataDungeons = wrapper.Bind(section, "Dungeon data", true, false, "Use dungeon data");
    configDataDungeons.SettingChanged += (s, e) => Dungeon.Loader.Load();
    configDataRooms = wrapper.Bind(section, "Room data", true, false, "Use dungeon room data");
    configDataRooms.SettingChanged += (s, e) => RoomLoading.Load();
    configDataWorld = wrapper.Bind(section, "World data", true, true, "Use world data");
    configDataWorld.SettingChanged += (s, e) => WorldManager.FromSetting(valueWorldData.Value);
    configDataLocation = wrapper.Bind(section, "Location data", true, false, "Use location data");
    configDataLocation.SettingChanged += (s, e) => LocationLoading.Load();
    configDataVegetation = wrapper.Bind(section, "Vegetation data", true, false, "Use vegetation data");
    configDataVegetation.SettingChanged += (s, e) => VegetationLoading.Load();
    configBlueprintFolder = wrapper.Bind(section, "Blueprint folder", "PlanBuild", false, "Folder relative to the config folder.");

    valueNoBuildData = wrapper.AddValue("no_build_data");
    valueNoBuildData.ValueChanged += () => NoBuildManager.Load(valueNoBuildData.Value);
    valueEnvironmentData = wrapper.AddValue("environment_data");
    valueEnvironmentData.ValueChanged += () => EnvironmentManager.FromSetting(valueEnvironmentData.Value);
    valueBiomeData = wrapper.AddValue("biome_data");
    valueBiomeData.ValueChanged += () => BiomeManager.FromSetting(valueBiomeData.Value);
    valueClutterData = wrapper.AddValue("clutter_data");
    valueClutterData.ValueChanged += () => ClutterManager.Set(valueClutterData.Value);
    valueWorldData = wrapper.AddValue("world_data");
    valueWorldData.ValueChanged += () => WorldManager.FromSetting(valueWorldData.Value);

    section = "4. Poles";
    configRestrictAshlands = wrapper.Bind(section, "Restrict Ashlands position", true, false, "If true, restricts Ashlands biome position.");
    configRestrictAshlands.SettingChanged += (s, e) => HandleRestrictAshlandsPosition();
    /*
    configAshlandsWidthRestriction = wrapper.BindFloat(section, "Ashlands width restriction", 7500f, false, "How many meters are the Ashlands biome width.");
    configAshlandsWidthRestriction.SettingChanged += (s, e) => HandleRestrictAshlandsPosition();
    configAshlandsDistanceRestriction = wrapper.BindFloat(section, "Ashlands distance restriction", 1000f, false, "How many meters are the Ashlands biome distance.");
    configAshlandsDistanceRestriction.SettingChanged += (s, e) => HandleRestrictAshlandsPosition();
    configAshlandsDepthRestriction = wrapper.BindFloat(section, "Ashlands depth restriction", 600f, false, "How many meters are the Ashlands biome depth.");
    configAshlandsDepthRestriction.SettingChanged += (s, e) => HandleRestrictAshlandsPosition();*/
    configAshlandsGap = wrapper.Bind(section, "Ashlands gap", true, false, "If true, Ashlands biome has an Ocean gap above it.");
    configAshlandsGap.SettingChanged += (s, e) => HandleAshlandsGap();
    configDeepNorthGap = wrapper.Bind(section, "Deep North gap", true, false, "If true, Deep North biome has an Ocean gap below it.");
    configDeepNorthGap.SettingChanged += (s, e) => HandleDeepNorthGap();
  }
}

[HarmonyPatch(typeof(ZNet))]
public class ZNetPatch
{
  // When the world is set on the server (applies to single player as well), we should select the correct loaded settings
  [HarmonyPrefix, HarmonyPatch(nameof(ZNet.SetServer))]
  private static void SetServerPrefix(bool server, World world)
  {
    if (server)
    {
      Configuration.HandleAshlandsGap();
      Configuration.HandleDeepNorthGap();
      Configuration.HandleRestrictAshlandsPosition();
    }
  }
}