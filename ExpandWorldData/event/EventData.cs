using System.Collections.Generic;
using System.ComponentModel;
using ExpandWorldData;
using Service;
using YamlDotNet.Serialization;

namespace ExpandWorld.Event;

public class Data
{
  public string name = "";
  [DefaultValue(true)] public bool enabled = true;
  [DefaultValue(0f)] public float customChance;
  [DefaultValue(0f)] public float customInterval;
  [DefaultValue(60f)] public float duration = 60f;
  [DefaultValue(96f)] public float radius = 96f;
  [DefaultValue(0f)] public float spawnerDelay;
  [DefaultValue("false")] public string outsideBaseOnly = "false";
  [DefaultValue("true")] public string nearBaseOnly = "true";
  [DefaultValue("")] public string biome = "";
  [DefaultValue("")] public string requiredGlobalKeys = "";
  [DefaultValue("")] public string notRequiredGlobalKeys = "";
  [DefaultValue("")] public string requiredPlayerKeys = "";
  [DefaultValue("")] public string requiredPlayerKeysAll = "";
  [DefaultValue("")] public string notRequiredPlayerKeys = "";
  [DefaultValue("")] public string requiredKnownItems = "";
  [DefaultValue("")] public string notRequiredKnownItems = "";
  [DefaultValue("")] public string requiredEnvironments = "";
  [DefaultValue("")] public string startMessage = "";
  [DefaultValue("")] public string endMessage = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public string forceMusic = "";
  [DefaultValue("")] public string forceEnvironment = "";
  [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
  public Spawn.Data[] spawns = [];
  [DefaultValue(true)] public bool pauseIfNoPlayerInArea = true;
  [DefaultValue(true)] public bool random = true;
  [DefaultValue(100f)] public float playerDistance = 100f;
  [DefaultValue("")] public string playerLimit = "";
  [DefaultValue("")] public string eventLimit = "";
  [DefaultValue(null)] public string[]? startCommands;
  [DefaultValue(null)] public string[]? endCommands;
}

public class ExtraData
{
  public List<string> RequiredEnvironments = [];
  public float PlayerDistance = 100f;
  public Range<int>? PlayerLimit;
  public int MinBaseValue = 3;
  public int MaxBaseValue = int.MaxValue;
  public Range<int>? EventLimit;
  public string[]? StartCommands;
  public string[]? EndCommands;

  public ExtraData(Data data)
  {
    RequiredEnvironments = DataManager.ToList(data.requiredEnvironments);
    PlayerDistance = data.playerDistance;
    if (data.playerLimit != "") PlayerLimit = Parse.IntRange(data.playerLimit);
    if (data.eventLimit != "") EventLimit = Parse.IntRange(data.eventLimit);
    if (data.nearBaseOnly != "true") MinBaseValue = Parse.Int(data.nearBaseOnly, 0);
    if (data.outsideBaseOnly != "false") { MaxBaseValue = Parse.Int(data.outsideBaseOnly, 2); MinBaseValue = 0; }
    StartCommands = data.startCommands;
    EndCommands = data.endCommands;
  }
}