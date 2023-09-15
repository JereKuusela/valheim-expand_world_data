using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class SpawnManager
{
  public static string FileName = "expand_spawns.yaml";
  public static string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  public static string Pattern = "expand_spawns*.yaml";
  public static Dictionary<SpawnSystem.SpawnData, List<BlueprintObject>> Objects = [];
  public static List<SpawnSystem.SpawnData> Originals = [];

  public static SpawnSystem.SpawnData FromData(SpawnData data)
  {
    SpawnSystem.SpawnData spawn = new()
    {
      m_prefab = DataManager.ToPrefab(data.prefab),
      m_enabled = data.enabled,
      m_biome = DataManager.ToBiomes(data.biome),
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea),
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
      m_overrideLevelupChance = data.overrideLevelupChance
    };
    if (spawn.m_minAltitude == -10000f)
      spawn.m_minAltitude = spawn.m_maxAltitude > 0f ? 0f : -1000f;
    if (data.data != "")
    {
      Data[spawn] = ZDOData.Create(data.data);
    }
    if (data.objects != null)
    {
      Objects[spawn] = data.objects.Select(s => Parse.Split(s)).Select(split => new BlueprintObject(
        split[0],
        Parse.VectorXZY(split, 1),
        Quaternion.identity,
        Vector3.one,
        ZDOData.Create(split.Length > 5 ? split[5] : ""),
        Parse.Float(split, 4, 1f)
      )).ToList();
    }
    return spawn;
  }
  public static SpawnData ToData(SpawnSystem.SpawnData spawn)
  {
    SpawnData data = new()
    {
      prefab = spawn.m_prefab.name,
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
      overrideLevelupChance = spawn.m_overrideLevelupChance
    };
    return data;
  }
  public static bool IsValid(SpawnSystem.SpawnData spawn) => spawn.m_prefab;
  public static string Save()
  {
    var spawnSystem = SpawnSystem.m_instances.FirstOrDefault();
    if (spawnSystem == null) return "";
    var spawns = spawnSystem.m_spawnLists.SelectMany(s => s.m_spawners);
    var yaml = DataManager.Serializer().Serialize(spawns.Select(ToData).ToList());
    File.WriteAllText(FilePath, yaml);
    return yaml;
  }
  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataSpawns) return;
    if (Originals.Count == 0)
    {
      var spawnSystem = SpawnSystem.m_instances.FirstOrDefault();
      Originals = spawnSystem.m_spawnLists.SelectMany(s => s.m_spawners).ToList();
    }
    if (File.Exists(FilePath)) return;
    var yaml = Save();
    Configuration.valueSpawnData.Value = yaml;
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = Configuration.DataSpawns ? DataManager.Read(Pattern) : "";
    Configuration.valueSpawnData.Value = yaml;
    Set(yaml);
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  private static void Set(string yaml)
  {
    HandleSpawnData.Override = null;

    Data.Clear();
    Objects.Clear();
    if (yaml == "" || !Configuration.DataSpawns) return;
    try
    {
      var data = DataManager.Deserialize<SpawnData>(yaml, FileName)
        .Select(FromData).Where(IsValid).ToList();
      if (data.Count == 0)
      {
        EWD.Log.LogWarning($"Failed to load any spawn data.");
        return;
      }
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data))
      {
        // Watcher triggers reload.
        return;
      }
      EWD.Log.LogInfo($"Reloading spawn data ({data.Count} entries).");
      HandleSpawnData.Override = data;
      foreach (var system in SpawnSystem.m_instances)
      {
        system.m_spawnLists.Clear();
        system.m_spawnLists.Add(new() { m_spawners = data });
      }
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
  }

  private static bool AddMissingEntries(List<SpawnSystem.SpawnData> entries)
  {
    var missingKeys = Originals.Select(s => s.m_prefab.name).Distinct().ToHashSet();
    foreach (var item in entries)
      missingKeys.Remove(item.m_prefab.name);
    if (missingKeys.Count == 0) return false;
    var missing = Originals.Where(item => missingKeys.Contains(item.m_prefab.name)).ToList();
    EWD.Log.LogWarning($"Adding {missing.Count} missing spawns to the expand_spawns.yaml file.");
    foreach (var item in missing)
      EWD.Log.LogWarning(item.m_prefab.name);
    var yaml = File.ReadAllText(FilePath);
    var data = DataManager.Serializer().Serialize(missing.Select(ToData));
    // Directly appending is risky but necessary to keep comments, etc.
    yaml += "\n" + data;
    File.WriteAllText(FilePath, yaml);
    return true;
  }
  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, FromFile);
  }

  public static Dictionary<SpawnSystem.SpawnData, ZDOData?> Data = new();

}


[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
public class SpawnZDO
{
  static void Prefix(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
  {
    if (!SpawnManager.Data.TryGetValue(critter, out var data)) return;
    if (!critter.m_prefab.TryGetComponent<ZNetView>(out var view)) return;
    DataHelper.InitZDO(spawnPoint, Quaternion.identity, null, data, view);
  }

  private static string PrefabOverride(string prefab)
  {
    return prefab;
  }
  static ZDOData? DataOverride(ZDOData? data, string prefab) => data;
  static void Postfix(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
  {
    if (!SpawnManager.Objects.TryGetValue(critter, out var objects)) return;
    foreach (var obj in objects)
    {
      if (obj.Chance < 1f && UnityEngine.Random.value > obj.Chance) continue;
      Spawn.BPO(obj, spawnPoint, Quaternion.identity, Vector3.one, DataOverride, PrefabOverride, null);
    }
  }
}
