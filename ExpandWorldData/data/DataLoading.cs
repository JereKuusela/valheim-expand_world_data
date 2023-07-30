using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class DataLoading
{
  private static readonly string FileName = "expand_data.yaml";
  private static readonly string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  private static readonly string Pattern = "expand_data*.yaml";


  public static void Initialize()
  {
    Load();
  }
  public static void Load()
  {
    if (Helper.IsClient()) return;
    if (!File.Exists(FilePath))
    {
      var yaml = DataManager.Serializer().Serialize(DefaultData.Data);
      File.WriteAllText(FilePath, yaml);
      // Watcher triggers reload.
      return;
    }

    var data = FromFile();
    if (data.Count == 0)
    {
      EWD.Log.LogWarning($"Failed to load any data data.");
      return;
    }
    EWD.Log.LogInfo($"Reloading data ({data.Count} entries).");
    foreach (var item in data)
      ZDOData.Register(item);
  }
  ///<summary>Loads all yaml files returning the deserialized vegetation entries.</summary>
  private static List<ZDOData> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return DataManager.Deserialize<DataData>(yaml, FileName).Select(FromData)
        .Where(data => data.Name != "").ToList();
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
    return new();
  }

  public static ZDOData FromData(DataData data)
  {
    ZDOData zdo = new()
    {
      Name = data.name
    };
    foreach (var value in data.floats ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 2) continue;
      zdo.Floats.Add(split[0].GetStableHashCode(), Parse.Float(split[1]));
    }
    foreach (var value in data.ints ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 2) continue;
      zdo.Ints.Add(split[0].GetStableHashCode(), Parse.Int(split[1]));
    }
    foreach (var value in data.longs ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 2) continue;
      zdo.Longs.Add(split[0].GetStableHashCode(), Parse.Long(split[1]));
    }
    foreach (var value in data.strings ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 2) continue;
      zdo.Strings.Add(split[0].GetStableHashCode(), split[1]);
    }
    foreach (var value in data.vecs ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 4) continue;
      zdo.Vecs.Add(split[0].GetStableHashCode(), Parse.VectorXZY(split, 1));
    }
    foreach (var value in data.quats ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 4) continue;
      zdo.Quats.Add(split[0].GetStableHashCode(), Parse.AngleYXZ(split, 1));
    }
    return zdo;
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
      scaleMin = veg.m_scaleMin.ToString(CultureInfo.InvariantCulture),
      scaleMax = veg.m_scaleMax.ToString(CultureInfo.InvariantCulture),
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
      forestTresholdMax = veg.m_forestTresholdMax
    };
    return data;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
  }
}
