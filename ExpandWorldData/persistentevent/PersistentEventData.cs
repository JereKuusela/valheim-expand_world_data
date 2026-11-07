using System.Collections.Generic;
using System.ComponentModel;
using Data;
using Service;
using YamlDotNet.Serialization;

namespace ExpandWorldData.PersistentEvents;

public class PersistentEventYaml
{
  public string name = "";
  [DefaultValue(true)] public bool enabled = true;
  [DefaultValue("$unknown_event")] public string mapToken = "$unknown_event";
  [DefaultValue(100f)] public float minRadius = 100f;
  [DefaultValue(300f)] public float maxRadius = 300f;
  [DefaultValue(-1)] public int maxConcurrent = -1;
  [DefaultValue("")] public string location = "";
  // Seconds. Both zero means the event never expires.
  [DefaultValue(0f)] public float minDuration;
  [DefaultValue(0f)] public float maxDuration;
  [DefaultValue("")] public string biome = "";
  [DefaultValue(1000f)] public float minDistance = 1000f;
  [DefaultValue(8000f)] public float maxDistance = 8000f;
  [DefaultValue(0f)] public float minAltitude;
  [DefaultValue(970f)] public float maxAltitude = 970f;
  [DefaultValue(45f)] public float maxSlope = 45f;
  [DefaultValue(400f)] public float minDistanceFromSimilar = 400f;
  [DefaultValue("")] public string environment = "";
  [DefaultValue(false)] public bool perBiomeEnvironments;
  [DefaultValue("")] public string graphicalEffects = "";
  [DefaultValue(0f)] public float customChance;
  [DefaultValue(0f)] public float customInterval;
  [DefaultValue(null)] public StringList? requiredGlobalKeys = null;
  [DefaultValue(null)] public StringList? forbiddenGlobalKeys = null;
  [DefaultValue("")] public string startMessage = "";
  [DefaultValue("")] public string endMessage = "";
  [DefaultValue(null)] public string[]? startCommands;
  [DefaultValue(null)] public string[]? endCommands;
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public PersistentEventObjectYaml[] objects = [];
}

public class PersistentEventObjectYaml
{
  // Prefab or blueprint names, one is picked randomly.
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public string[] prefabs = [];
  [DefaultValue("")] public string data = "";
  [DefaultValue(false)] public bool important;
  [DefaultValue(true)] public bool randomRotation = true;
  [DefaultValue(0)] public int minCount;
  [DefaultValue(10)] public int maxCount = 10;
  [DefaultValue(10f)] public float minDistanceFromOtherObjects = 10f;
  [DefaultValue(100f)] public float maxDistance = 100f;
  [DefaultValue(90f)] public float maxSlope = 90f;
  [DefaultValue("CanSpawnInWater")] public string water = "CanSpawnInWater";
}

public class PersistentEventExtra(PersistentEventYaml data)
{
  public bool Enabled = data.enabled;
  public float Chance = data.customChance;
  public float Interval = data.customInterval;
  public List<string[]>? RequiredKeys = StringList.ToRows(data.requiredGlobalKeys);
  public List<string[]>? ForbiddenKeys = StringList.ToRows(data.forbiddenGlobalKeys);
  public string StartMessage = data.startMessage;
  public string EndMessage = data.endMessage;
  public string[]? StartCommands = data.startCommands;
  public string[]? EndCommands = data.endCommands;
}

public class PersistentEventObjectExtra(string[] prefabs, DataEntry? data)
{
  public string[] Prefabs = prefabs;
  public DataEntry? Data = data;
}
