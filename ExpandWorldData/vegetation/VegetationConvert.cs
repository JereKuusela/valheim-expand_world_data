using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Common;
using Data;
using UnityEngine;

namespace ExpandWorldData;

public partial class VegetationManager
{
  public static ZoneSystem.ZoneVegetation FromData(VegetationYaml data, string fileName)
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
      m_altBiomeParent = string.IsNullOrWhiteSpace(data.altBiome) ? null : data.altBiome,
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
    ValueRange<Vector3> scale = new(Parse.Scale(data.scaleMin), Parse.Scale(data.scaleMax))
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
    if (data.data != null)
      extra.data = DataHelper.Get(data.data, fileName);
    if (data.drops != "")
    {
      DataEntry dropsEntry = new()
      {
        Hashes = new Dictionary<int, IHashValue> { { HashDrop, DataValue.Hash(data.drops) } }
      };
      extra.data = DataHelper.Merge(extra.data, dropsEntry);
    }


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
      extra.requiredGlobalKeys = Service.StringList.ToRows(data.requiredGlobalKey);
      extra.forbiddenGlobalKeys = Service.StringList.ToRows(data.forbiddenGlobalKey);
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

  public static VegetationYaml ToData(ZoneSystem.ZoneVegetation veg) => new()
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
    altBiome = veg.m_altBiomeParent ?? "",
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
}
