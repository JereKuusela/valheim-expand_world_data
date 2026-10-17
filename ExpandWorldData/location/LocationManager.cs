using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;
using Data;
using Common;
namespace ExpandWorldData;

public partial class LocationManager
{
  public static string FileName = "expand_locations.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_locations*.yaml";
  public static bool Customized { get; private set; }
  private static readonly List<LocationYaml> ExtraLocationYamls = [];
  public static void AddLocation(LocationYaml yaml)
  {
    ExtraLocationYamls.Add(yaml);
  }

  public static bool IsValid(ZoneSystem.ZoneLocation loc) => loc.m_prefab.IsValid;

  private static void ToFile()
  {
    var data = DefaultEntries.Where(IsValid).Select(ToData).ToList();
    if (ExtraLocationYamls.Count > 0)
      data.AddRange(ExtraLocationYamls);
    Save(data, false);
  }
  public static void Initialize()
  {
    ZoneSystem.instance.m_locations = [.. ZoneSystem.instance.m_locations.Where(loc => loc.m_prefab.IsValid)];

    Customized = false;
    DefaultEntries.Clear();
    Locations.Clear();
    if (Helper.IsServer())
    {
      DefaultEntries = ZoneSystem.instance.m_locations;
      Locations = Helper.ToDict(DefaultEntries, l => l.m_prefab.Name, l => l);
    }
  }

  private class Sync : YamlDataManager
  {
    protected override string FilePath => LocationManager.FilePath;
    protected override string Pattern => LocationManager.Pattern;
    protected override bool DataEnabled => Configuration.DataLocation;
    protected override SetResult Set(Dictionary<string, string> files) => LocationManager.Set(files);
    protected override void WriteDefaultConfig() => ToFile();
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void CleanUp()
  {
    LocationExtra.ClearInfo();
  }

  private static SetResult Set(Dictionary<string, string> files)
  {
    var previousInfo = LocationExtra.ExtraInfo;
    var previousVirtual = LocationExtra.ExtraInfoByVirtualId;
    LocationExtra.ExtraInfo = [];
    LocationExtra.ExtraInfoByVirtualId = [];
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(files);
    }
    finally
    {
      if (result != SetResult.Applied)
      {
        LocationExtra.ExtraInfo = previousInfo;
        LocationExtra.ExtraInfoByVirtualId = previousVirtual;
      }
    }
  }
  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    var result = Load(files, out var data);
    if (result != SetResult.Applied) return result;
    Customized = !ReferenceEquals(data, DefaultEntries);
    ZoneSystem.instance.m_locations = data;
    UpdateHashes();
    UpdateInstances();
    CreateLocalZones.LocationsPregenerated = false;
    NoBuildManager.UpdateData();
    LocationClientData.UpdateData();
    MinimapIcon.Clear();
    // Refresh.Request is deferred; update icon patches before broadcasting the new configuration.
    DataPatcher.PatchLocationIcons(EWD.Harmony);
    ZoneSystem.instance.SendLocationIcons(ZRoutedRpc.Everybody);
    IdManager.SendLocationIds();
    return SetResult.Applied;
  }
  private static SetResult Load(Dictionary<string, string> files, out List<ZoneSystem.ZoneLocation> data)
  {
    data = DefaultEntries;
    if (files.Count == 0)
    {
      Log.Info($"Reloading default location data ({DefaultEntries.Count} entries).");
      return SetResult.Applied;
    }
    List<ZoneSystem.ZoneLocation> parsed = [];
    foreach (var file in files)
    {
      if (!Yaml.TryDeserialize<LocationYaml>(file.Value, file.Key, out var items)) return SetResult.Failed;
      parsed.AddRange(items.Select(item => FromData(item, file.Key)).Where(loc => !string.IsNullOrWhiteSpace(loc.m_prefab.Name)));
    }
    if (parsed.Count == 0)
    {
      Log.Warning($"Failed to load any location data.");
      return SetResult.Failed;
    }
    if (YamlDataManager.MigrationEnabled &&
      (AltBiomeMigration.MigrateLocations(parsed, DefaultEntries, Pattern)
      || LocationMigration.Migrate(parsed, Locations, Pattern)
      || AddMissingEntries(parsed)))
      return SetResult.Rewritten;
    data = parsed;
    Log.Info($"Reloading location data ({parsed.Count} entries).");
    return SetResult.Applied;
  }
  private static void UpdateHashes()
  {
    var zs = ZoneSystem.instance;
    zs.m_locationsByHash.Clear();
    foreach (var loc in zs.m_locations)
      zs.m_locationsByHash[loc.m_prefab.Name.GetStableHashCode()] = loc;
  }
  private static void UpdateInstances()
  {
    var zs = ZoneSystem.instance;
    var instances = zs.m_locationInstances;
    foreach (var zone in instances.Keys.ToArray())
    {
      var value = instances[zone];
      var location = zs.m_locations.FirstOrDefault(location => location.m_prefab.Name == value.m_location.m_prefab.Name);
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
    YamlFiles.SavePerMod("locations", data, item => item.prefab, log);
  }

  private static void ApplyLocationData(ZoneSystem.ZoneLocation item, float? radius = null)
  {
    if (!LocationExtra.TryGetData(item, out var data)) return;
    // Zero means that the vanilla value is used.
    if (data.interiorRadius > 0f) item.m_interiorRadius = data.interiorRadius;
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
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataLocation, ReadConfigs);
  }
}
