using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;
using Data;
namespace ExpandWorldData;

public class LocationLoading
{
  public static string FileName = "expand_locations.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_locations*.yaml";
  private static readonly List<LocationYaml> ExtraLocationYamls = [];
  public static void AddLocation(LocationYaml yaml)
  {
    ExtraLocationYamls.Add(yaml);
  }
  public static ZoneSystem.ZoneLocation FromData(LocationYaml data, string fileName)
  {
    var loc = new ZoneSystem.ZoneLocation
    {
      m_prefabName = data.prefab,

      m_enable = data.enabled,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea, fileName),
      m_quantity = data.quantity,
      m_prioritized = data.prioritized,
      m_centerFirst = data.centerFirst,
      m_unique = data.unique,
      m_group = data.group,
      m_minDistanceFromSimilar = data.minDistanceFromSimilar,
      m_iconAlways = data.iconAlways != "" && data.iconAlways != "false",
      m_iconPlaced = data.iconPlaced != "" && data.iconPlaced != "false",
      m_randomRotation = data.randomRotation,
      m_slopeRotation = data.slopeRotation,
      m_snapToWater = data.snapToWater,
      m_minTerrainDelta = data.minTerrainDelta,
      m_maxTerrainDelta = data.maxTerrainDelta,
      m_inForest = data.inForest,
      m_forestTresholdMin = data.forestTresholdMin,
      m_forestTresholdMax = data.forestTresholdMax,
      m_minDistance = WorldEntry.ConvertDist(data.minDistance),
      m_maxDistance = WorldEntry.ConvertDist(data.maxDistance),
      m_minAltitude = data.minAltitude,
      m_maxAltitude = data.maxAltitude,
      m_groupMax = data.groupMax,
      m_maxDistanceFromSimilar = data.maxDistanceFromSimilar,
      m_minimumVegetation = data.minVegetation,
      m_maximumVegetation = data.maxVegetation,
      m_surroundCheckVegetation = data.surroundCheckVegetation,
      m_surroundCheckDistance = data.surroundCheckDistance,
      m_surroundCheckLayers = data.surroundCheckLayers,
      m_surroundBetterThanAverage = data.surroundBetterThanAverage
    };
    LocationExtra.AddInfo(loc, data, fileName);

    Setup(data.prefab, loc, fileName);
    return loc;
  }

  public static LocationYaml ToData(ZoneSystem.ZoneLocation loc)
  {
    LocationYaml data = new();
    // For migrations, ensures that old data is preserved.
    if (LocationExtra.TryGetData(loc, out var extra))
      data = extra;
    // Original game has two fields for min/max distance from center. This merges the implementations.
    if (loc.m_minDistanceFromCenter != 0f && (loc.m_minDistance == 0f || loc.m_minDistanceFromCenter > loc.m_minDistance))
      loc.m_minDistance = loc.m_minDistanceFromCenter;
    if (loc.m_maxDistanceFromCenter != 0f && (loc.m_maxDistance == 0f || loc.m_maxDistanceFromCenter < loc.m_maxDistance))
      loc.m_maxDistance = loc.m_maxDistanceFromCenter;
    data.prefab = loc.m_prefab.Name;
    data.enabled = loc.m_enable;
    data.biome = DataManager.FromBiomes(loc.m_biome);
    data.biomeArea = DataManager.FromBiomeAreas(loc.m_biomeArea);
    data.quantity = loc.m_quantity;
    data.prioritized = loc.m_prioritized;
    data.centerFirst = loc.m_centerFirst;
    data.unique = loc.m_unique;
    data.group = loc.m_group;
    data.minDistanceFromSimilar = loc.m_minDistanceFromSimilar;
    data.iconAlways = loc.m_iconAlways ? loc.m_prefab.Name : "";
    data.iconPlaced = loc.m_iconPlaced ? loc.m_prefab.Name : "";
    data.randomRotation = loc.m_randomRotation;
    data.slopeRotation = loc.m_slopeRotation;
    data.snapToWater = loc.m_snapToWater;
    data.maxTerrainDelta = loc.m_maxTerrainDelta;
    data.minTerrainDelta = loc.m_minTerrainDelta;
    data.inForest = loc.m_inForest;
    data.forestTresholdMin = loc.m_forestTresholdMin;
    data.forestTresholdMax = loc.m_forestTresholdMax;
    data.minDistance = loc.m_minDistance / 10000f;
    data.maxDistance = loc.m_maxDistance / 10000f;
    data.minAltitude = loc.m_minAltitude;
    data.groupMax = loc.m_groupMax;
    data.maxDistanceFromSimilar = loc.m_maxDistanceFromSimilar;
    data.minVegetation = loc.m_minimumVegetation;
    data.maxVegetation = loc.m_maximumVegetation;
    data.surroundCheckVegetation = loc.m_surroundCheckVegetation;
    data.surroundCheckDistance = loc.m_surroundCheckDistance;
    data.surroundCheckLayers = loc.m_surroundCheckLayers;
    data.surroundBetterThanAverage = loc.m_surroundBetterThanAverage;
    if (loc.m_maxAltitude == 1000f)
      data.maxAltitude = 10000f;
    else
      data.maxAltitude = loc.m_maxAltitude;
    var asset = Helper.SafeLoad(loc);
    if (asset == null)
      return data;
    var prefab = asset.GetComponent<Location>();
    if (prefab)
    {
      data.randomDamage = prefab.m_applyRandomDamage ? "true" : "";
      data.exteriorRadius = prefab.m_exteriorRadius;
      data.clearArea = prefab.m_clearArea;
      data.discoverLabel = prefab.m_discoverLabel;
      data.noBuild = prefab.m_noBuild ? "true" : "";
      if (prefab.m_noBuild && prefab.m_noBuildRadiusOverride > 0f)
        data.noBuild = prefab.m_noBuildRadiusOverride.ToString(NumberFormatInfo.InvariantInfo);
    }
    loc.m_prefab.Release();
    return data;
  }
  public static bool IsValid(ZoneSystem.ZoneLocation loc) => loc.m_prefab.IsValid;

  private static void ToFile()
  {
    if (File.Exists(FilePath)) return;
    var data = ZoneSystem.instance.m_locations.Where(IsValid).Select(ToData).ToList();
    if (ExtraLocationYamls.Count > 0)
      data.AddRange(ExtraLocationYamls);
    Save(data, false);
  }
  public static void Initialize()
  {
    DefaultEntries.Clear();
    Locations.Clear();
    if (Helper.IsServer())
    {
      DefaultEntries = ZoneSystem.instance.m_locations;
      Locations = Helper.ToDict(DefaultEntries, l => l.m_prefab.Name, l => l);
    }
    Load();
  }
  public static void Load()
  {
    LocationExtra.ClearInfo();
    if (Helper.IsClient()) return;
    ZoneSystem.instance.m_locations = DefaultEntries;
    if (Configuration.DataLocation)
    {
      if (!File.Exists(FilePath))
      {
        ToFile();
        // Watcher triggers reload.
        return;
      }
      var data = FromFile();
      if (data.Count == 0)
      {
        Log.Warning($"Failed to load any location data.");
        Log.Info($"Reloading default location data ({DefaultEntries.Count} entries).");
      }
      else
      {
        if (Configuration.DataMigration && AddMissingEntries(data))
        {
          // Watcher triggers reload.
          return;
        }
        ZoneSystem.instance.m_locations = data;
        Log.Info($"Reloading location data ({data.Count} entries).");
      }
    }
    else
      Log.Info($"Reloading default location data ({DefaultEntries.Count} entries).");
    UpdateHashes();
    UpdateInstances();
    CreateLocalZones.LocationsPregenerated = false;
    NoBuildManager.UpdateData();
    MinimapIcon.Clear();
    ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
    IdManager.SendLocationIds();
  }
  private static void UpdateHashes()
  {
    var zs = ZoneSystem.instance;
    zs.m_locationsByHash = Helper.ToDict(zs.m_locations, loc => loc.m_prefab.Name.GetStableHashCode(), loc => loc);
    //ExpandWorldData.Log.Debug($"Loaded {zs.m_locationsByHash.Count} zone hashes.");
  }
  private static void UpdateInstances()
  {
    var zs = ZoneSystem.m_instance;
    var instances = zs.m_locationInstances;
    foreach (var zone in instances.Keys.ToArray())
    {
      var value = instances[zone];
      var location = zs.GetLocation(value.m_location.m_prefab.Name);
      // Jewelcrafting has dynamic locations that don't exist in the location list.
      if (location == null) continue;
      value.m_location = location;
      instances[zone] = value;
    }
  }
  // Dictionary can't be used because some mods might have multiple entries for the same location.
  private static List<ZoneSystem.ZoneLocation> DefaultEntries = [];
  // Used to optimize missing entries check (to avoid n^2 loop).
  // Also used to quickly check if a location is blueprint or not.
  private static Dictionary<string, ZoneSystem.ZoneLocation> Locations = [];
  private static bool AddMissingEntries(List<ZoneSystem.ZoneLocation> items)
  {
    var missingKeys = Locations.Keys.ToHashSet();
    foreach (var item in items)
      missingKeys.Remove(item.m_prefab.Name);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(loc => missingKeys.Contains(loc.m_prefab.Name)).Select(ToData).ToList();
    Log.Warning($"Adding {missing.Count} missing locations to the expand_locations.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<LocationYaml> data, bool log)
  {
    Dictionary<string, List<LocationYaml>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.prefab);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.prefab}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_locations{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value);
      File.WriteAllText(file, yaml);
    }
  }

  private static List<ZoneSystem.ZoneLocation> FromFile()
  {
    try
    {
      return DataManager.ReadData<LocationYaml, ZoneSystem.ZoneLocation>(Pattern, FromData)
        .Where(loc => !string.IsNullOrWhiteSpace(loc.m_prefab.Name)).ToList();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    return [];
  }
  private static void ApplyLocationData(ZoneSystem.ZoneLocation item, float? radius = null)
  {
    if (!LocationExtra.TryGetData(item, out var data)) return;
    // Old config won't have exterior radius so don't set anything.
    if (data.exteriorRadius == 0f && radius == null) return;
    item.m_exteriorRadius = data.exteriorRadius;
    if (radius.HasValue && item.m_exteriorRadius == 0)
      item.m_exteriorRadius = radius.Value;
    item.m_clearArea = data.clearArea;
  }


  private static bool SetupBlueprint(string name, ZoneSystem.ZoneLocation location)
  {
    if (!BlueprintManager.TryGet(name, out var bp)) return false;

    location.m_prefab = new(new()) { m_name = name };
    ApplyLocationData(location, bp.Radius + 5);
    return true;
  }
  ///<summary>Copies setup from locations.</summary>
  private static void Setup(string name, ZoneSystem.ZoneLocation item, string fileName)
  {
    var baseName = Parse.Name(name);
    if (!Locations.TryGetValue(baseName, out var zoneLocation) || !zoneLocation.m_prefab.IsValid)
    {
      if (SetupBlueprint(name, item)) return;
      Log.Warning($"{fileName}: Location prefab {baseName} not found!");
      return;
    }
    item.m_prefab = new(zoneLocation.m_prefab.m_assetID) { m_name = name };
    item.m_interiorRadius = zoneLocation.m_interiorRadius;
    item.m_exteriorRadius = zoneLocation.m_exteriorRadius;
    ApplyLocationData(item);
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, Load);
  }
}


[HarmonyPatch(typeof(ZoneSystem))]
public static class ZoneSystemPatches
{
  [HarmonyPatch(nameof(ZoneSystem.GetLocationIcon))]
  [HarmonyPrefix]
  static bool GetLocationIcon(ZoneSystem __instance, string name, ref Vector3 pos, ref bool __result)
  {
    if (!ZNet.instance.IsServer())
      return true;
    // Server should also use GetLocationIcons so that single player matches the dedicated server behavior.
    __instance.tempIconList.Clear();
    __instance.GetLocationIcons(__instance.tempIconList);
    foreach (var kvp in __instance.tempIconList)
    {
      if (kvp.Value != name)
        continue;
      pos = kvp.Key;
      __result = true;
      return false;
    }
    pos = Vector3.zero;
    __result = false;
    return false;
  }

  [HarmonyPatch(nameof(ZoneSystem.GetLocationIcons))]
  [HarmonyPrefix]
  static bool GetLocationIcons(ZoneSystem __instance, Dictionary<Vector3, string> icons)
  {
    if (!Configuration.DataLocation) return true;
    if (!ZNet.instance.IsServer()) return true;
    foreach (var kvp in __instance.m_locationInstances)
    {
      var loc = kvp.Value.m_location;
      var pos = kvp.Value.m_position;
      if (loc == null) continue;
      if (LocationExtra.TryGetData(loc, out var data))
      {
        var placed = data.iconPlaced == "true" ? loc.m_prefab.Name : data.iconPlaced == "false" ? "" : data.iconPlaced;
        if (kvp.Value.m_placed && placed != "")
        {
          icons[pos] = placed;
        }
        else
        {
          pos.y += 0.00001f; // Trivial amount for a different key.
          var always = data.iconAlways == "true" ? loc.m_prefab.Name : data.iconAlways == "false" ? "" : data.iconAlways;
          if (always != "") icons[pos] = always;
        }
      }
      // Jewelcrafting has dynamic locations so use the vanilla code as a fallback.
      else if (loc.m_iconAlways || (loc.m_iconPlaced && kvp.Value.m_placed))
      {
        icons[pos] = loc.m_prefab.Name;
      }
    }
    return false;
  }

  // Vanilla checks that location prefabs are valid.
  // This doesn't work with blueprints because they would need proper asset ID (which might conflict with other mods).
  [HarmonyPatch(nameof(ZoneSystem.GetLocation), typeof(string))]
  [HarmonyPrefix]
  static bool GetLocation(ZoneSystem __instance, string name, ref ZoneSystem.ZoneLocation __result)
  {
    foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
    {
      if (zoneLocation.m_prefab.Name == name)
      {
        __result = zoneLocation;
        return false;
      }
    }
    __result = null!;
    return false;
  }
}