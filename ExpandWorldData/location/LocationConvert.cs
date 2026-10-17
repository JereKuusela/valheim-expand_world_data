using System.Globalization;
using System.Linq;
using Common;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public partial class LocationManager
{
  public static ZoneSystem.ZoneLocation FromData(LocationYaml data, string fileName)
  {
    var loc = new ZoneSystem.ZoneLocation
    {
      m_prefabName = data.prefab,

      m_enable = data.enabled,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea, fileName),
      m_altBiomeParent = string.IsNullOrWhiteSpace(data.altBiome) ? null : data.altBiome,
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
    data.altBiome = loc.m_altBiomeParent ?? "";
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
      FillClientFields(data, prefab);
    }
    loc.m_prefab.Release();
    return data;
  }

  internal static void FillClientFields(LocationYaml data, Location prefab)
  {
    data.noBuild = prefab.m_noBuild ? "true" : "";
    if (prefab.m_noBuild && prefab.m_noBuildRadiusOverride > 0f)
      data.noBuild = prefab.m_noBuildRadiusOverride.ToString(NumberFormatInfo.InvariantInfo);
    data.interiorRadius = prefab.m_hasInterior ? prefab.m_interiorRadius : 0f;
    data.interiorEnvironment = prefab.m_interiorEnvironment;
    data.enemyMinLevel = prefab.m_enemyMinLevelOverride;
    data.enemyMaxLevel = prefab.m_enemyMaxLevelOverride;
    data.enemyLevelUpChance = prefab.m_enemyLevelUpOverride;
    data.enemyLevelExcludeGroups = string.Join(",", prefab.m_excludeEnemyLevelOverrideGroups);
    data.blockSpawnGroups = string.Join(",", prefab.m_blockSpawnGroups);
  }
}
