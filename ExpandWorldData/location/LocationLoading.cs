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
  public static Dictionary<string, string> ZDOData = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, string>>>> LocationObjectSwaps = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, string>>>> DungeonObjectSwaps = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, DataEntry?>>>> LocationObjectData = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, DataEntry?>>>> DungeonObjectData = [];
  public static Dictionary<string, List<BlueprintObject>> Objects = [];
  public static Dictionary<string, Range<Vector3>> Scales = [];
  public static Dictionary<string, string[]> Commands = [];
  public static Dictionary<string, LocationData> LocationData = [];
  public static Dictionary<string, string> Dungeons = [];
  public static ZoneSystem.ZoneLocation FromData(LocationData data)
  {
    var loc = new ZoneSystem.ZoneLocation();
    LocationData[data.prefab] = data;
    loc.m_prefabName = data.prefab;
    if (data.data != "")
      ZDOData[data.prefab] = data.data;
    if (data.dungeon != "")
      Dungeons[data.prefab] = data.dungeon;
    LoadObjectData(data);
    LoadObjectSwaps(data);
    if (data.objects != null)
      Objects[data.prefab] = Helper.ParseObjects(data.objects);
    if (data.commands != null)
    {
      Commands[data.prefab] = data.commands;
    }


    Range<Vector3> scale = new(Parse.Scale(data.scaleMin), Parse.Scale(data.scaleMax))
    {
      Uniform = data.scaleUniform
    };
    if (scale.Min != scale.Max)
      Scales[data.prefab] = scale;

    loc.m_enable = data.enabled;
    loc.m_biome = DataManager.ToBiomes(data.biome);
    loc.m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea);
    loc.m_quantity = data.quantity;
    loc.m_prioritized = data.prioritized;
    loc.m_centerFirst = data.centerFirst;
    loc.m_unique = data.unique;
    loc.m_group = data.group;
    loc.m_minDistanceFromSimilar = data.minDistanceFromSimilar;
    loc.m_iconAlways = data.iconAlways != "" && data.iconAlways != "false";
    loc.m_iconPlaced = data.iconPlaced != "" && data.iconPlaced != "false";
    loc.m_randomRotation = data.randomRotation;
    loc.m_slopeRotation = data.slopeRotation;
    loc.m_snapToWater = data.snapToWater;
    loc.m_minTerrainDelta = data.minTerrainDelta;
    loc.m_maxTerrainDelta = data.maxTerrainDelta;
    loc.m_inForest = data.inForest;
    loc.m_forestTresholdMin = data.forestTresholdMin;
    loc.m_forestTresholdMax = data.forestTresholdMax;
    loc.m_minDistance = data.minDistance * 10000f;
    loc.m_maxDistance = data.maxDistance * 10000f;
    loc.m_minAltitude = data.minAltitude;
    loc.m_maxAltitude = data.maxAltitude;
    loc.m_groupMax = data.groupMax;
    loc.m_maxDistanceFromSimilar = data.maxDistanceFromSimilar;
    loc.m_minimumVegetation = data.minVegetation;
    loc.m_maximumVegetation = data.maxVegetation;
    loc.m_surroundCheckVegetation = data.surroundCheckVegetation;
    loc.m_surroundCheckDistance = data.surroundCheckDistance;
    loc.m_surroundCheckLayers = data.surroundCheckLayers;
    loc.m_surroundBetterThanAverage = data.surroundBetterThanAverage;

    Setup(data.prefab, loc);
    return loc;
  }

  private static void LoadObjectData(LocationData data)
  {
    Dictionary<string, List<Tuple<float, DataEntry?>>>? locationobjectData = null;
    Dictionary<string, List<Tuple<float, DataEntry?>>>? dungeonobjectData = null;

    if (data.objectData != null)
    {
      locationobjectData = Spawn.LoadData(data.objectData);
      dungeonobjectData = Spawn.LoadData(data.objectData);
    }
    if (data.locationObjectData != null)
    {
      var objectData = Spawn.LoadData(data.locationObjectData);
      if (locationobjectData == null)
      {
        locationobjectData = objectData;
      }
      else
      {
        foreach (var kvp in objectData)
          locationobjectData[kvp.Key] = kvp.Value;
      }
    }
    if (data.dungeonObjectData != null)
    {
      var objectData = Spawn.LoadData(data.dungeonObjectData);
      if (dungeonobjectData == null)
      {
        dungeonobjectData = objectData;
      }
      else
      {
        foreach (var kvp in objectData)
          dungeonobjectData[kvp.Key] = kvp.Value;
      }
    }
    if (dungeonobjectData != null)
      DungeonObjectData[data.prefab] = dungeonobjectData;
    if (locationobjectData != null)
      LocationObjectData[data.prefab] = locationobjectData;
  }
  private static void LoadObjectSwaps(LocationData data)
  {
    Dictionary<string, List<Tuple<float, string>>>? locationobjectSwaps = null;
    Dictionary<string, List<Tuple<float, string>>>? dungeonobjectSwaps = null;

    if (data.objectSwap != null)
    {
      locationobjectSwaps = Spawn.LoadSwaps(data.objectSwap);
      dungeonobjectSwaps = Spawn.LoadSwaps(data.objectSwap);
    }
    if (data.locationObjectSwap != null)
    {
      var objectSwap = Spawn.LoadSwaps(data.locationObjectSwap);
      if (locationobjectSwaps == null)
      {
        locationobjectSwaps = objectSwap;
      }
      else
      {
        foreach (var kvp in objectSwap)
          locationobjectSwaps[kvp.Key] = kvp.Value;
      }
    }
    if (data.dungeonObjectSwap != null)
    {
      var objectSwap = Spawn.LoadSwaps(data.dungeonObjectSwap);
      if (dungeonobjectSwaps == null)
      {
        dungeonobjectSwaps = objectSwap;
      }
      else
      {
        foreach (var kvp in objectSwap)
          dungeonobjectSwaps[kvp.Key] = kvp.Value;
      }
    }
    if (dungeonobjectSwaps != null)
      DungeonObjectSwaps[data.prefab] = dungeonobjectSwaps;
    if (locationobjectSwaps != null)
      LocationObjectSwaps[data.prefab] = locationobjectSwaps;
  }

  public static LocationData ToData(ZoneSystem.ZoneLocation loc)
  {
    LocationData data = new();
    // For migrations, ensures that old data is preserved.
    if (LocationData.TryGetValue(loc.m_prefab.Name, out var existing))
      data = existing;
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
    loc.m_prefab.Load();
    var prefab = loc.m_prefab.Asset.GetComponent<Location>();
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
    Save(ZoneSystem.instance.m_locations.Where(IsValid).ToList(), false);
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
    ZDOData.Clear();
    LocationObjectSwaps.Clear();
    DungeonObjectSwaps.Clear();
    LocationObjectData.Clear();
    DungeonObjectData.Clear();
    Dungeons.Clear();
    Objects.Clear();
    Scales.Clear();
    Commands.Clear();
    LocationData.Clear();
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
    NoBuildManager.UpdateData();
    MinimapIcon.Clear();
    ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
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
    Dictionary<string, List<ZoneSystem.ZoneLocation>> perFile = [];
    var missingKeys = Locations.Keys.ToHashSet();
    foreach (var item in items)
      missingKeys.Remove(item.m_prefab.Name);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(loc => missingKeys.Contains(loc.m_prefab.Name)).ToList();
    Log.Warning($"Adding {missing.Count} missing locations to the expand_locations.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<ZoneSystem.ZoneLocation> data, bool log)
  {
    Dictionary<string, List<ZoneSystem.ZoneLocation>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.m_prefab.Name);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.m_prefab.Name}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_locations{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value.Select(ToData));
      File.WriteAllText(file, yaml);
    }
  }

  private static List<ZoneSystem.ZoneLocation> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return Yaml.Deserialize<LocationData>(yaml, FileName).Select(FromData)
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
    if (!LocationData.TryGetValue(item.m_prefab.Name, out var data)) return;
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
  private static void Setup(string name, ZoneSystem.ZoneLocation item)
  {
    var baseName = Parse.Name(name);
    if (!Locations.TryGetValue(baseName, out var zoneLocation) || !zoneLocation.m_prefab.IsValid)
    {
      if (SetupBlueprint(name, item)) return;
      Log.Warning($"Location prefab {baseName} not found!");
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

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetLocationIcons))]
public class LocationIcons
{
  static bool Prefix(ZoneSystem __instance, Dictionary<Vector3, string> icons)
  {
    if (!Configuration.DataLocation) return true;
    if (!ZNet.instance.IsServer()) return true;
    foreach (var kvp in __instance.m_locationInstances)
    {
      var loc = kvp.Value.m_location;
      var pos = kvp.Value.m_position;
      if (loc == null) continue;
      if (LocationLoading.LocationData.TryGetValue(loc.m_prefab.Name, out var data))
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
}

// Vanilla checks that location prefabs are valid.
// This doesn't work with blueprints because they would need proper asset ID (which might conflict with other mods).
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetLocation), typeof(string))]
public class GetLocation
{
  static bool Prefix(ZoneSystem __instance, string name, ref ZoneSystem.ZoneLocation __result)
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