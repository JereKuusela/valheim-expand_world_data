using System.Collections.Generic;
using System.ComponentModel;
using YamlDotNet.Serialization;

namespace ExpandWorld.Spawn;

public class Data
{
  public string prefab = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public bool enabled = true;
  [DefaultValue("")] public string name = "";
  [DefaultValue("")] public string drops = "";
  [DefaultValue("")] public string biome = "";
  [DefaultValue("")] public string biomeArea = "";
  [DefaultValue(100f)] public float spawnChance = 100f;
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public int maxSpawned = 1;
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public float spawnInterval = 0f;
  [DefaultValue(1)] public int maxLevel = 1;
  [DefaultValue(1)] public int minLevel = 1;
  [DefaultValue(-10000f)] public float minAltitude = -10000f;
  [DefaultValue(10000f)] public float maxAltitude = 10000f;
  [DefaultValue(true)] public bool spawnAtDay = true;
  [DefaultValue(true)] public bool spawnAtNight = true;
  [DefaultValue("")] public string requiredGlobalKey = "";
  [DefaultValue("")] public string requiredEnvironments = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public float spawnDistance = 10f;
  [DefaultValue(0f)] public float spawnRadiusMin = 0f;
  [DefaultValue(0f)] public float spawnRadiusMax = 0f;
  [DefaultValue(1)] public int groupSizeMin = 1;
  [DefaultValue(1)] public int groupSizeMax = 1;
  [DefaultValue(0f)] public float groupRadius = 0f;
  [DefaultValue(0f)] public float minTilt = 0f;
  [DefaultValue(35f)] public float maxTilt = 35f;
  [DefaultValue(true)] public bool inForest = true;
  [DefaultValue(true)] public bool outsideForest = true;
  [DefaultValue(false)] public bool canSpawnCloseToPlayer = false;
  [DefaultValue(false)] public bool insidePlayerBase = false;
  [DefaultValue(false)] public bool inLava = false;
  [DefaultValue(true)] public bool outsideLava = true;
  [DefaultValue(0f)] public float minOceanDepth = 0f;
  [DefaultValue(0f)] public float maxOceanDepth = 0f;
  [DefaultValue(false)] public bool huntPlayer = false;
  [DefaultValue(0.5f)] public float groundOffset = 0.5f;
  [DefaultValue(0f)] public float groundOffsetRandom = 0f;
  [DefaultValue(0f)] public float levelUpMinCenterDistance = 0f;
  [DefaultValue(-1f)] public float overrideLevelupChance = -1f;
  [DefaultValue(0f)] public float minDistance = 0f;
  [DefaultValue(0f)] public float maxDistance = 0f;
  [DefaultValue(null)] public string[]? objects = null;
  [DefaultValue(null)] public string? data = null;
  [DefaultValue(null)] public string? faction = null;
  [DefaultValue(null)] public Dictionary<string, string>? fields = null;
}