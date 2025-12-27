using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Data;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class VegetationLoading
{
  private static readonly string FileName = "expand_vegetation.yaml";
  private static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  private static readonly string Pattern = "expand_vegetation*.yaml";


  // Default items are stored to track missing entries.
  private static List<ZoneSystem.ZoneVegetation> DefaultEntries = [];
  public static void Initialize()
  {
    DefaultEntries.Clear();
    DefaultKeys.Clear();
    if (Helper.IsServer())
      SetDefaultEntries();
    Load();
  }
  public static void Load()
  {
    VegetationSpawning.Extra.Clear();
    VegetationSpawning.Prefabs.Clear();
    if (Helper.IsClient()) return;
    ZoneSystem.instance.m_vegetation = DefaultEntries;
    if (!Configuration.DataVegetation)
    {
      Log.Info($"Reloading default vegetation data ({DefaultEntries.Count} entries).");
      return;
    }
    if (!File.Exists(FilePath))
    {
      Save(DefaultEntries, false);
      // Watcher triggers reload.
      return;
    }

    var data = FromFile();
    if (data.Count == 0)
    {
      Log.Warning($"Failed to load any vegetation data.");
      Log.Info($"Reloading default vegetation data ({DefaultEntries.Count} entries).");
      return;
    }
    if (Configuration.DataMigration && AddMissingEntries(data))
    {
      // Watcher triggers reload.
      return;
    }
    Log.Info($"Reloading vegetation data ({data.Count} entries).");
    ZoneSystem.instance.m_vegetation = data;
    IdManager.SendVegetationIds();

  }
  ///<summary>Loads all yaml files returning the deserialized vegetation entries.</summary>
  private static List<ZoneSystem.ZoneVegetation> FromFile()
  {
    try
    {
      return DataManager.ReadData<VegetationData, ZoneSystem.ZoneVegetation>(Pattern, FromData)
        .Where(veg => veg.m_prefab).ToList();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    return [];
  }
  ///<summary>Cleans up default vegetation data and stores it to track missing entries.</summary>
  private static void SetDefaultEntries()
  {
    ZoneSystem.instance.m_vegetation = ZoneSystem.instance.m_vegetation
      .Where(veg => veg.m_prefab)
      .Where(veg => ZNetScene.instance.m_namedPrefabs.ContainsKey(veg.m_prefab.name.GetStableHashCode()))
      .Where(veg => veg.m_enable && veg.m_max > 0f).ToList();
    DefaultEntries = ZoneSystem.instance.m_vegetation;
    DefaultKeys = Helper.ToSet(DefaultEntries, veg => veg.m_prefab.name);
  }
  // Used to optimize missing entries check (to avoid n^2 loop).
  private static HashSet<string> DefaultKeys = [];

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  // Note: This is needed people add new content mods and then complain that Expand World doesn't spawn them.
  private static bool AddMissingEntries(List<ZoneSystem.ZoneVegetation> entries)
  {
    Dictionary<string, List<ZoneSystem.ZoneVegetation>> perFile = [];
    var missingKeys = DefaultKeys.ToHashSet();
    // Some mods override prefabs so the m_prefab.name is not reliable.
    foreach (var entry in entries)
    {
      missingKeys.Remove(entry.m_name);
      if (VegetationSpawning.Prefabs.TryGetValue(entry, out var prefabs))
        missingKeys.RemoveWhere(key => prefabs.Any(prefab => prefab.name == key));
    }
    if (missingKeys.Count == 0) return false;
    // But don't use m_name because it can be anything for original items.
    var missing = DefaultEntries.Where(veg => missingKeys.Contains(veg.m_prefab.name)).ToList();
    Log.Warning($"Adding {missing.Count} missing vegetation to the expand_vegetation.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<ZoneSystem.ZoneVegetation> data, bool log)
  {
    Dictionary<string, List<ZoneSystem.ZoneVegetation>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.m_prefab.name);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.m_prefab.name}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_vegetation{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value.Select(ToData));
      File.WriteAllText(file, yaml);
    }
  }

  public static ZoneSystem.ZoneVegetation FromData(VegetationData data, string fileName)
  {
    if (data.minDistance > 0f)
      data.minDistance = WorldEntry.ConvertDist(data.minDistance);
    if (data.maxDistance > 0f)
      data.maxDistance = WorldEntry.ConvertDist(data.maxDistance);
    ZoneSystem.ZoneVegetation veg = new()
    {
      m_name = data.prefab,
      m_enable = data.enabled,
      m_min = data.min,
      m_max = data.max,
      m_forcePlacement = data.forcePlacement,
      m_scaleMin = Parse.Scale(data.scaleMin).x,
      m_scaleMax = Parse.Scale(data.scaleMax).x,
      m_randTilt = data.randTilt,
      m_chanceToUseGroundTilt = data.chanceToUseGroundTilt,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea, fileName),
      m_blockCheck = data.blockCheck,
      m_minAltitude = data.minAltitude,
      m_maxAltitude = data.maxAltitude,
      m_minOceanDepth = data.minOceanDepth,
      m_maxOceanDepth = data.maxOceanDepth,
      m_minVegetation = data.minVegetation,
      m_maxVegetation = data.maxVegetation,
      m_minTilt = data.minTilt,
      m_maxTilt = data.maxTilt,
      m_terrainDeltaRadius = data.terrainDeltaRadius,
      m_maxTerrainDelta = data.maxTerrainDelta,
      m_minTerrainDelta = data.minTerrainDelta,
      m_snapToWater = data.snapToWater,
      m_snapToStaticSolid = data.snapToStaticSolid,
      m_groundOffset = data.groundOffset,
      m_groupSizeMin = data.groupSizeMin,
      m_groupSizeMax = data.groupSizeMax,
      m_groupRadius = data.groupRadius,
      m_inForest = data.inForest,
      m_forestTresholdMin = data.forestTresholdMin,
      m_forestTresholdMax = data.forestTresholdMax,
      m_minDistanceFromCenter = data.minDistance,
      m_maxDistanceFromCenter = data.maxDistance,
    };
    Range<Vector3> scale = new(Parse.Scale(data.scaleMin), Parse.Scale(data.scaleMax))
    {
      Uniform = data.scaleUniform
    };
    VegetationExtra extra = new()
    {
      clearRadius = data.clearRadius,
      clearArea = data.clearArea,
    };
    // Minor optimization to skip RNG calls if there is nothing to randomize.
    if (Helper.IsMultiAxis(scale))
      extra.scale = scale;
    if (data.data != "")
      extra.data = DataHelper.Get(data.data, fileName);


    var prefabs = DataManager.ToList(data.prefab).Select(p =>
    {
      var hash = p.GetStableHashCode();
      if (ZNetScene.instance.m_namedPrefabs.TryGetValue(hash, out var obj))
        return obj;
      if (BlueprintManager.Load(data.prefab))
        return new(data.prefab);
      return null!;
    }).Where(p => p).ToList();


    if (prefabs.Count > 0)
      veg.m_prefab = prefabs[0];
    if (prefabs.Count > 1)
      VegetationSpawning.Prefabs.Add(veg, prefabs);

    if (veg.m_enable)
    {
      if (data.requiredGlobalKey != "")
        extra.requiredGlobalKeys = DataManager.ToList(data.requiredGlobalKey);
      if (data.forbiddenGlobalKey != "")
        extra.forbiddenGlobalKeys = DataManager.ToList(data.forbiddenGlobalKey);
      if (data.centerX != 0f || data.centerY != 0f)
      {
        // Center is not supported in the original game, so to have to fallback to the custom check.
        veg.m_minDistanceFromCenter = 0;
        veg.m_maxDistanceFromCenter = 0;
        extra.center = new(WorldEntry.ConvertDist(data.centerX), WorldEntry.ConvertDist(data.centerY));
        if (data.minDistance != 0f || data.maxDistance != 0f)
          extra.distance = new(data.minDistance, data.maxDistance);
      }
    }
    if (extra.IsValid())
      VegetationSpawning.Extra.Add(veg, extra);
    return veg;
  }
  public static VegetationData ToData(ZoneSystem.ZoneVegetation veg)
  {
    VegetationData data = new()
    {
      enabled = veg.m_enable,
      prefab = veg.m_prefab.name,
      min = veg.m_min,
      max = veg.m_max,
      forcePlacement = veg.m_forcePlacement,
      scaleMin = veg.m_scaleMin.ToString(NumberFormatInfo.InvariantInfo),
      scaleMax = veg.m_scaleMax.ToString(NumberFormatInfo.InvariantInfo),
      randTilt = veg.m_randTilt,
      chanceToUseGroundTilt = veg.m_chanceToUseGroundTilt,
      biome = DataManager.FromBiomes(veg.m_biome),
      biomeArea = DataManager.FromBiomeAreas(veg.m_biomeArea),
      blockCheck = veg.m_blockCheck,
      minAltitude = veg.m_minAltitude,
      maxAltitude = veg.m_maxAltitude,
      minOceanDepth = veg.m_minOceanDepth,
      maxOceanDepth = veg.m_maxOceanDepth,
      minVegetation = veg.m_minVegetation,
      maxVegetation = veg.m_maxVegetation,
      minTilt = veg.m_minTilt,
      maxTilt = veg.m_maxTilt,
      terrainDeltaRadius = veg.m_terrainDeltaRadius,
      maxTerrainDelta = veg.m_maxTerrainDelta,
      minTerrainDelta = veg.m_minTerrainDelta,
      snapToWater = veg.m_snapToWater,
      snapToStaticSolid = veg.m_snapToStaticSolid,
      groundOffset = veg.m_groundOffset,
      groupSizeMin = veg.m_groupSizeMin,
      groupSizeMax = veg.m_groupSizeMax,
      groupRadius = veg.m_groupRadius,
      inForest = veg.m_inForest,
      forestTresholdMin = veg.m_forestTresholdMin,
      forestTresholdMax = veg.m_forestTresholdMax,
      surroundCheckVegetation = veg.m_surroundCheckVegetation,
      surroundCheckDistance = veg.m_surroundCheckDistance,
      surroundCheckLayers = veg.m_surroundCheckLayers,
      surroundBetterThanAverage = veg.m_surroundBetterThanAverage,
      maxDistance = veg.m_maxDistanceFromCenter / 10000f,
      minDistance = veg.m_minDistanceFromCenter / 10000f,
    };
    return data;
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, Load);
  }
}
