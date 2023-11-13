using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ExpandWorldData;

public class LocationLoading
{
  public static string FileName = "expand_locations.yaml";
  public static string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  public static string Pattern = "expand_locations*.yaml";
  public static Dictionary<string, string> ZDOData = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, string>>>> ObjectSwaps = [];
  public static Dictionary<string, Dictionary<string, List<Tuple<float, ZDOData?>>>> ObjectData = [];
  public static Dictionary<string, List<BlueprintObject>> Objects = [];
  public static Dictionary<string, Range<Vector3>> Scales = [];
  public static Dictionary<string, string[]> Commands = [];
  public static Dictionary<string, LocationData> LocationData = [];
  public static Dictionary<string, string> Dungeons = [];
  public static Dictionary<string, Location> BlueprintLocations = [];
  public static ZoneSystem.ZoneLocation FromData(LocationData data)
  {
    var loc = new ZoneSystem.ZoneLocation();
    LocationData[data.prefab] = data;
    loc.m_prefabName = data.prefab;
    if (!Locations.ContainsKey(Parse.Name(data.prefab)))
    {
      if (!BlueprintManager.Load(data.prefab, data.centerPiece))
        loc.m_prefabName = "";
    }

    if (data.data != "")
      ZDOData[data.prefab] = data.data;
    if (data.dungeon != "")
      Dungeons[data.prefab] = data.dungeon;
    if (data.objectSwap != null)
      ObjectSwaps[data.prefab] = Spawn.LoadSwaps(data.objectSwap);
    if (data.objectData != null)
      ObjectData[data.prefab] = Spawn.LoadData(data.objectData);
    if (data.objects != null)
      Objects[data.prefab] = Parse.Objects(data.objects);
    if (data.commands != null)
      Commands[data.prefab] = data.commands;

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
    return loc;
  }


  public static LocationData ToData(ZoneSystem.ZoneLocation loc)
  {
    LocationData data = new();
    // For migrations, ensures that old data is preserved.
    if (LocationData.TryGetValue(loc.m_prefabName, out var existing))
      data = existing;
    data.prefab = loc.m_prefabName;
    data.enabled = loc.m_enable;
    data.biome = DataManager.FromBiomes(loc.m_biome);
    data.biomeArea = DataManager.FromBiomeAreas(loc.m_biomeArea);
    data.quantity = loc.m_quantity;
    data.prioritized = loc.m_prioritized;
    data.centerFirst = loc.m_centerFirst;
    data.unique = loc.m_unique;
    data.group = loc.m_group;
    data.minDistanceFromSimilar = loc.m_minDistanceFromSimilar;
    data.iconAlways = loc.m_iconAlways ? loc.m_prefabName : "";
    data.iconPlaced = loc.m_iconPlaced ? loc.m_prefabName : "";
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
    if (loc.m_maxAltitude == 1000f)
      data.maxAltitude = 10000f;
    else
      data.maxAltitude = loc.m_maxAltitude;
    if (loc.m_location)
    {
      data.randomDamage = loc.m_location.m_applyRandomDamage ? "true" : "";
      data.exteriorRadius = loc.m_location.m_exteriorRadius;
      data.clearArea = loc.m_location.m_clearArea;
      data.discoverLabel = loc.m_location.m_discoverLabel;
      data.noBuild = loc.m_location.m_noBuild ? "true" : "";
      if (loc.m_location.m_noBuild && loc.m_location.m_noBuildRadiusOverride > 0f)
        data.noBuild = loc.m_location.m_noBuildRadiusOverride.ToString(NumberFormatInfo.InvariantInfo);
    }
    return data;
  }
  public static bool IsValid(ZoneSystem.ZoneLocation loc) => loc.m_prefab;

  private static void ToFile()
  {
    if (File.Exists(FilePath)) return;
    var yaml = DataManager.Serializer().Serialize(ZoneSystem.instance.m_locations.Where(IsValid).Select(ToData).ToList());
    File.WriteAllText(FilePath, yaml);
  }
  public static void Initialize()
  {
    DefaultEntries.Clear();
    Locations.Clear();
    if (Helper.IsServer())
    {
      DefaultEntries = ZoneSystem.instance.m_locations;
      Locations = Helper.ToDict(DefaultEntries, l => l.m_prefabName, l => l);
    }
    Load();
  }
  public static void Load()
  {
    ZDOData.Clear();
    ObjectSwaps.Clear();
    ObjectData.Clear();
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
        EWD.Log.LogWarning($"Failed to load any location data.");
        EWD.Log.LogInfo($"Reloading default location data ({DefaultEntries.Count} entries).");
      }
      else
      {
        if (Configuration.DataMigration && AddMissingEntries(data))
        {
          // Watcher triggers reload.
          return;
        }
        ZoneSystem.instance.m_locations = data;
        EWD.Log.LogInfo($"Reloading location data ({data.Count} entries).");
      }
    }
    else
      EWD.Log.LogInfo($"Reloading default location data ({DefaultEntries.Count} entries).");
    foreach (var item in ZoneSystem.instance.m_locations) Setup(item);
    LocationSetup.UpdateHashes();
    UpdateInstances();
    NoBuildManager.UpdateData();
    ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
    CleanMap();
  }
  private static void CleanMap()
  {
    var mm = Minimap.instance;
    if (!mm) return;
    foreach (var pin in mm.m_locationPins)
      mm.RemovePin(pin.Value);
    mm.m_locationPins.Clear();
  }
  private static void UpdateInstances()
  {
    var zs = ZoneSystem.m_instance;
    var instances = zs.m_locationInstances;
    foreach (var zone in instances.Keys.ToArray())
    {
      var value = instances[zone];
      var location = zs.GetLocation(value.m_location.m_prefabName);
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
      missingKeys.Remove(item.m_prefabName);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(loc => missingKeys.Contains(loc.m_prefabName)).ToList();
    EWD.Log.LogWarning($"Adding {missing.Count} missing locations to the expand_locations.yaml file.");
    foreach (var item in missing)
      EWD.Log.LogWarning(item);
    var yaml = File.ReadAllText(FilePath);
    var data = DataManager.Serializer().Serialize(missing.Select(ToData));
    // Directly appending is risky but necessary to keep comments, etc.
    yaml += "\n" + data;
    File.WriteAllText(FilePath, yaml);
    return true;
  }
  private static List<ZoneSystem.ZoneLocation> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return DataManager.Deserialize<LocationData>(yaml, FileName).Select(FromData)
        .Where(loc => loc.m_prefabName != "").ToList();
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
    return [];
  }
  private static void ApplyLocationData(ZoneSystem.ZoneLocation item, float? radius = null)
  {
    if (!LocationData.TryGetValue(item.m_prefabName, out var data)) return;
    // Old config won't have exterior radius so don't set anything.
    if (data.exteriorRadius == 0f && radius == null) return;
    item.m_location.m_exteriorRadius = data.exteriorRadius;
    item.m_exteriorRadius = item.m_location.m_exteriorRadius;
    if (radius.HasValue && item.m_exteriorRadius == 0)
      item.m_exteriorRadius = radius.Value;
    item.m_location.m_applyRandomDamage = data.randomDamage == "true" || data.randomDamage == "all";
    item.m_location.m_clearArea = data.clearArea;
    // Handled by own system.
    item.m_location.m_noBuild = false;
  }
  public static Location GetBluePrintLocation(string prefab)
  {
    if (!BlueprintLocations.TryGetValue(prefab, out var location))
    {
      var obj = new GameObject
      {
        name = "Blueprint"
      };
      location = obj.AddComponent<Location>();
      BlueprintLocations.Add(prefab, location);
    }
    return location;
  }


  private static bool SetupBlueprint(ZoneSystem.ZoneLocation location)
  {
    if (!BlueprintManager.TryGet(location.m_prefabName, out var bp)) return false;
    location.m_prefab = new();
    location.m_location = GetBluePrintLocation(location.m_prefabName);
    ApplyLocationData(location, bp.Radius + 5);
    location.m_netViews = [];
    location.m_randomSpawns = [];
    return true;
  }
  ///<summary>Copies setup from locations.</summary>
  private static void Setup(ZoneSystem.ZoneLocation item)
  {
    var prefabName = Parse.Name(item.m_prefabName);
    item.m_hash = item.m_prefabName.GetStableHashCode();
    if (!Locations.TryGetValue(prefabName, out var zoneLocation) || zoneLocation.m_prefab == null || zoneLocation.m_location == null)
    {
      if (SetupBlueprint(item)) return;
      EWD.Log.LogWarning($"Location prefab {prefabName} not found!");
      return;
    }
    item.m_prefab = zoneLocation.m_prefab;
    item.m_location = zoneLocation.m_location;
    item.m_interiorRadius = zoneLocation.m_interiorRadius;
    item.m_exteriorRadius = zoneLocation.m_exteriorRadius;
    item.m_interiorPosition = zoneLocation.m_interiorPosition;
    item.m_generatorPosition = zoneLocation.m_generatorPosition;
    ApplyLocationData(item);
    item.m_netViews = zoneLocation.m_netViews;
    item.m_randomSpawns = zoneLocation.m_randomSpawns;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
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
      if (LocationLoading.LocationData.TryGetValue(loc.m_prefabName, out var data))
      {
        var placed = data.iconPlaced == "true" ? loc.m_prefabName : data.iconPlaced == "false" ? "" : data.iconPlaced;
        if (kvp.Value.m_placed && placed != "")
        {
          icons[pos] = placed;
        }
        else
        {
          pos.y += 0.00001f; // Trivial amount for a different key.
          var always = data.iconAlways == "true" ? loc.m_prefabName : data.iconAlways == "false" ? "" : data.iconAlways;
          if (always != "") icons[pos] = always;
        }
      }
      // Jewelcrafting has dynamic locations so use the vanilla code as a fallback.
      else if (loc.m_iconAlways || (loc.m_iconPlaced && kvp.Value.m_placed))
      {
        icons[pos] = loc.m_prefabName;
      }
    }
    return false;
  }
}


[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetLocationIcon))]
public class NewLocationIcons
{
  static Sprite Postfix(Sprite result, string name)
  {
    if (result != null) return result;
    if (Enum.TryParse<Minimap.PinType>(name, true, out var icon))
      return Minimap.instance.GetSprite(icon);
    var hash = name.GetStableHashCode();
    if (ObjectDB.instance.m_itemByHash.TryGetValue(hash, out var item))
      return item.GetComponent<ItemDrop>()?.m_itemData?.GetIcon()!;
    var effect = ObjectDB.instance.GetStatusEffect(hash);
    if (effect) return effect.m_icon;
    return null!;
  }
}