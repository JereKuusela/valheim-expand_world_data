using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using ExpandWorldData.Data;
using Service;
using UnityEngine;

namespace ExpandWorldData.PersistentEvents;

public class PersistentEventLoader
{
  public static Dictionary<PersistentEventSystem.PersistentEvent, PersistentEventExtra> Extra = [];
  public static Dictionary<PersistentEventSystem.ObjectSpawnSettings, PersistentEventObjectExtra> ObjectExtra = [];

  public static PersistentEventSystem.PersistentEvent FromData(PersistentEventYaml data, string fileName)
  {
    var hasDuration = data.minDuration > 0f || data.maxDuration > 0f;
    PersistentEventSystem.PersistentEvent result = new()
    {
      internalName = data.name,
      mapTokenString = data.mapToken,
      minRadius = data.minRadius,
      maxRadius = Mathf.Max(data.minRadius, data.maxRadius),
      maxConcurrent = data.maxConcurrent,
      locationToSpawn = data.location,
      hasDuration = hasDuration,
      minDurationInSeconds = data.minDuration,
      maxDurationInSeconds = Mathf.Max(data.minDuration, data.maxDuration),
      objectsToSpawn = [.. data.objects.Select(obj => FromObject(obj, fileName))],
      biomes = DataManager.ToBiomes(data.biome, fileName),
      minDistanceFromCenter = data.minDistance,
      maxDistanceFromCenter = data.maxDistance,
      minHeight = Helper.AltitudeToHeight(data.minAltitude),
      maxHeight = Helper.AltitudeToHeight(data.maxAltitude),
      maxSlope = data.maxSlope,
      minDistanceFromSimilar = data.minDistanceFromSimilar,
      environmentOverride = data.environment,
      perBiomeEnvironments = data.perBiomeEnvironments,
      graphicalEffects = ParseEffects(data.graphicalEffects, fileName),
    };
    Extra[result] = new(data);
    return result;
  }

  private static PersistentEventSystem.ObjectSpawnSettings FromObject(PersistentEventObjectYaml data, string fileName)
  {
    PersistentEventSystem.ObjectSpawnSettings result = new()
    {
      prefabs = [.. data.prefabs.Select(name => ZNetScene.instance.GetPrefab(name)).Where(prefab => prefab)],
      important = data.important,
      randomRotation = data.randomRotation,
      minCount = data.minCount,
      maxCount = data.maxCount,
      minDistanceFromOtherEventObjects = data.minDistanceFromOtherObjects,
      maxDistanceFromEventCenter = data.maxDistance,
      maxSlope = data.maxSlope,
      waterSpawning = ParseWater(data.water, fileName),
    };
    ObjectExtra[result] = new(data.prefabs, DataHelper.Get(data.data, fileName));
    return result;
  }

  private static PersistentEventSystem.EventGraphicalEffects ParseEffects(string value, string fileName)
  {
    if (value == "") return 0;
    if (Enum.TryParse<PersistentEventSystem.EventGraphicalEffects>(value, true, out var result)) return result;
    Log.Warning($"{fileName}: Invalid graphicalEffects {value}.");
    return 0;
  }

  private static PersistentEventSystem.ObjectSpawnSettings.WaterSpawnRule ParseWater(string value, string fileName)
  {
    if (Enum.TryParse<PersistentEventSystem.ObjectSpawnSettings.WaterSpawnRule>(value, true, out var result)) return result;
    Log.Warning($"{fileName}: Invalid water rule {value}.");
    return PersistentEventSystem.ObjectSpawnSettings.WaterSpawnRule.CanSpawnInWater;
  }

  public static PersistentEventYaml ToData(PersistentEventSystem.PersistentEvent ev) => new()
  {
    name = ev.internalName,
    mapToken = ev.mapTokenString,
    minRadius = ev.minRadius,
    maxRadius = ev.maxRadius,
    maxConcurrent = ev.maxConcurrent,
    location = ev.locationToSpawn ?? "",
    minDuration = ev.hasDuration ? ev.minDurationInSeconds : 0f,
    maxDuration = ev.hasDuration ? ev.maxDurationInSeconds : 0f,
    biome = DataManager.FromBiomes(ev.biomes),
    minDistance = ev.minDistanceFromCenter,
    maxDistance = ev.maxDistanceFromCenter,
    minAltitude = ev.minHeight - Helper.AltitudeToHeight(0f),
    maxAltitude = ev.maxHeight - Helper.AltitudeToHeight(0f),
    maxSlope = ev.maxSlope,
    minDistanceFromSimilar = ev.minDistanceFromSimilar,
    environment = ev.environmentOverride ?? "",
    perBiomeEnvironments = ev.perBiomeEnvironments,
    graphicalEffects = ev.graphicalEffects == 0 ? "" : ev.graphicalEffects.ToString(),
    objects = [.. ev.objectsToSpawn.Select(ToObjectData)],
  };

  private static PersistentEventObjectYaml ToObjectData(PersistentEventSystem.ObjectSpawnSettings obj) => new()
  {
    prefabs = [.. (obj.prefabs ?? []).Select(prefab => prefab.name)],
    important = obj.important,
    randomRotation = obj.randomRotation,
    minCount = obj.minCount,
    maxCount = obj.maxCount,
    minDistanceFromOtherObjects = obj.minDistanceFromOtherEventObjects,
    maxDistance = obj.maxDistanceFromEventCenter,
    maxSlope = obj.maxSlope,
    water = obj.waterSpawning.ToString(),
  };
}
