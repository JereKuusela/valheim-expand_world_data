using System.Collections.Generic;
using System.Linq;
using Data;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Spawn;

public class Loader
{
  public static Dictionary<SpawnSystem.SpawnData, DataEntry?> Data = [];
  public static Dictionary<SpawnSystem.SpawnData, List<BlueprintObject>> Objects = [];

  public static SpawnSystem.SpawnData FromData(Data data, string fileName)
  {
    SpawnSystem.SpawnData spawn = new()
    {
      m_prefab = DataManager.ToPrefab(data.prefab, fileName),
      m_name = data.name,
      m_enabled = data.enabled,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea, fileName),
      m_maxSpawned = data.maxSpawned,
      m_spawnInterval = data.spawnInterval,
      m_spawnChance = data.spawnChance,
      m_spawnDistance = data.spawnDistance,
      m_spawnRadiusMin = data.spawnRadiusMin,
      m_spawnRadiusMax = data.spawnRadiusMax,
      m_requiredGlobalKey = data.requiredGlobalKey,
      m_requiredEnvironments = DataManager.ToList(data.requiredEnvironments),
      m_groupSizeMin = data.groupSizeMin,
      m_groupSizeMax = data.groupSizeMax,
      m_spawnAtDay = data.spawnAtDay,
      m_spawnAtNight = data.spawnAtNight,
      m_groupRadius = data.groupRadius,
      m_minAltitude = data.minAltitude,
      m_maxAltitude = data.maxAltitude,
      m_minTilt = data.minTilt,
      m_maxTilt = data.maxTilt,
      m_inForest = data.inForest,
      m_outsideForest = data.outsideForest,
      m_minOceanDepth = data.minOceanDepth,
      m_maxOceanDepth = data.maxOceanDepth,
      m_huntPlayer = data.huntPlayer,
      m_groundOffset = data.groundOffset,
      m_maxLevel = data.maxLevel,
      m_minLevel = data.minLevel,
      m_levelUpMinCenterDistance = data.levelUpMinCenterDistance,
      m_overrideLevelupChance = data.overrideLevelupChance,
      m_inLava = data.inLava,
      m_outsideLava = data.outsideLava,
      m_canSpawnCloseToPlayer = data.canSpawnCloseToPlayer,
      m_insidePlayerBase = data.insidePlayerBase,
      m_groundOffsetRandom = data.groundOffsetRandom,
      m_minDistanceFromCenter = data.minDistance,
      m_maxDistanceFromCenter = data.maxDistance,
    };
    if (spawn.m_minAltitude == -10000f)
      spawn.m_minAltitude = spawn.m_maxAltitude > 0f ? 0f : -1000f;
    if (data.data != null)
      Data[spawn] = DataHelper.Get(data.data, fileName);
    var customData = LoaderFields.HandleCustomData(data, spawn);
    if (customData != null)
      Data[spawn] = Data.TryGetValue(spawn, out var existing) ? DataHelper.Merge(existing, customData) : customData;
    if (data.objects != null)
      Objects[spawn] = [.. data.objects.Select(value => Parse.Split(value)).Select(split => new BlueprintObject(
        split[0], Parse.VectorXZY(split, 1), Quaternion.identity, Vector3.one,
        DataHelper.Get(split.Length > 5 ? split[5] : "", fileName), Parse.Float(split, 4, 1f)))];
    return spawn;
  }

  public static Data ToData(SpawnSystem.SpawnData spawn) => new()
  {
    prefab = spawn.m_prefab.name,
    name = spawn.m_name,
    enabled = spawn.m_enabled,
    biome = DataManager.FromBiomes(spawn.m_biome),
    biomeArea = DataManager.FromBiomeAreas(spawn.m_biomeArea),
    maxSpawned = spawn.m_maxSpawned,
    spawnInterval = spawn.m_spawnInterval,
    spawnChance = spawn.m_spawnChance,
    spawnDistance = spawn.m_spawnDistance,
    spawnRadiusMin = spawn.m_spawnRadiusMin,
    spawnRadiusMax = spawn.m_spawnRadiusMax,
    requiredGlobalKey = spawn.m_requiredGlobalKey,
    requiredEnvironments = DataManager.FromList(spawn.m_requiredEnvironments),
    spawnAtDay = spawn.m_spawnAtDay,
    spawnAtNight = spawn.m_spawnAtNight,
    groupSizeMin = spawn.m_groupSizeMin,
    groupSizeMax = spawn.m_groupSizeMax,
    groupRadius = spawn.m_groupRadius,
    minAltitude = spawn.m_minAltitude,
    maxAltitude = spawn.m_maxAltitude,
    minTilt = spawn.m_minTilt,
    maxTilt = spawn.m_maxTilt,
    inForest = spawn.m_inForest,
    outsideForest = spawn.m_outsideForest,
    minOceanDepth = spawn.m_minOceanDepth,
    maxOceanDepth = spawn.m_maxOceanDepth,
    huntPlayer = spawn.m_huntPlayer,
    groundOffset = spawn.m_groundOffset,
    maxLevel = spawn.m_maxLevel,
    minLevel = spawn.m_minLevel,
    levelUpMinCenterDistance = spawn.m_levelUpMinCenterDistance,
    overrideLevelupChance = spawn.m_overrideLevelupChance,
    inLava = spawn.m_inLava,
    outsideLava = spawn.m_outsideLava,
    canSpawnCloseToPlayer = spawn.m_canSpawnCloseToPlayer,
    insidePlayerBase = spawn.m_insidePlayerBase,
    groundOffsetRandom = spawn.m_groundOffsetRandom,
    minDistance = spawn.m_minDistanceFromCenter,
    maxDistance = spawn.m_maxDistanceFromCenter,
  };

  internal static void ApplyData(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
  {
    if (Loader.Data.TryGetValue(critter, out var data))
      DataHelper.Init(critter.m_prefab, spawnPoint, Quaternion.identity, null, data);
  }

  internal static void SpawnObjects(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
  {
    if (!Loader.Objects.TryGetValue(critter, out var objects)) return;
    foreach (var obj in objects)
    {
      if (obj.Chance < 1f && Random.value > obj.Chance) continue;
      ExpandWorldData.Spawn.BPO(obj, spawnPoint, Quaternion.identity, Vector3.one, (data, prefab) => data, prefab => prefab, null);
    }
  }
}