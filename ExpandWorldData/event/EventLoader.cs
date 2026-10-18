using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;

namespace ExpandWorld.Event;

public class Loader
{
  public static Dictionary<RandomEvent, ExtraData> ExtraData = [];

  public static RandomEvent FromData(Data data, string fileName)
  {
    var keyRows = StringList.ToRows(data.requiredGlobalKeys) ?? [];
    var playerRows = StringList.ToRows(data.requiredPlayerKeys) ?? [];
    // Vanilla has "any of" and "all of" lists, which covers all single entry rows and one multi entry row.
    var anyRow = playerRows.FirstOrDefault(row => row.Length > 1);
    if (playerRows.Count(row => row.Length > 1) > 1)
      Log.Warning($"{fileName}: Event '{data.name}' has multiple requiredPlayerKeys rows with multiple entries. Only the first is used.");
    var forbiddenKeyRows = StringList.ToRows(data.forbiddenGlobalKeys);
    var forbiddenPlayerRows = StringList.ToRows(data.forbiddenPlayerKeys);
    // Vanilla forbidden lists are a single "any of" row.
    if (forbiddenPlayerRows is { Count: > 1 })
      Log.Warning($"{fileName}: Event '{data.name}' has multiple forbiddenPlayerKeys rows. Only the first is used.");
    RandomEvent random = new()
    {
      m_name = data.name,
      m_spawn = data.spawns.Select(entry => Spawn.Loader.FromData(entry, fileName)).Where(spawn => spawn.m_prefab).ToList(),
      m_enabled = data.enabled,
      m_random = data.random,
      m_duration = data.duration,
      m_nearBaseOnly = data.nearBaseOnly != "false",
      m_pauseIfNoPlayerInArea = data.pauseIfNoPlayerInArea,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_requiredGlobalKeys = [.. keyRows.Where(row => row.Length == 1).Select(row => row[0])],
      m_notRequiredGlobalKeys = forbiddenKeyRows is { Count: 1 } ? [.. forbiddenKeyRows[0]] : [],
      m_altRequiredPlayerKeysAny = anyRow == null ? [] : [.. anyRow],
      m_altRequiredPlayerKeysAll = [.. playerRows.Where(row => row.Length == 1).Select(row => row[0])],
      m_altNotRequiredPlayerKeys = forbiddenPlayerRows == null ? [] : [.. forbiddenPlayerRows[0]],
      m_altRequiredKnownItems = DataManager.ToItemList(data.requiredKnownItems),
      m_altRequiredNotKnownItems = DataManager.ToItemList(data.forbiddenKnownItems),
      m_startMessage = data.startMessage,
      m_endMessage = data.endMessage,
      m_forceMusic = data.forceMusic,
      m_forceEnvironment = data.forceEnvironment,
      m_eventRange = data.radius,
      m_standaloneChance = data.customChance,
      m_standaloneInterval = data.customInterval,
      m_spawnerDelay = data.spawnerDelay,
    };
    ExtraData[random] = new(data);
    return random;
  }

  public static Data ToData(RandomEvent random) => new()
  {
    name = random.m_name,
    spawns = random.m_spawn.Select(Spawn.Loader.ToData).ToArray(),
    enabled = random.m_enabled,
    random = random.m_random,
    duration = random.m_duration,
    nearBaseOnly = random.m_nearBaseOnly ? "true" : "false",
    pauseIfNoPlayerInArea = random.m_pauseIfNoPlayerInArea,
    biome = DataManager.FromBiomes(random.m_biome),
    requiredGlobalKeys = StringList.FromRows(random.m_requiredGlobalKeys),
    forbiddenGlobalKeys = StringList.FromRows([DataManager.FromList(random.m_notRequiredGlobalKeys)]),
    requiredPlayerKeys = StringList.FromRows([DataManager.FromList(random.m_altRequiredPlayerKeysAny), .. random.m_altRequiredPlayerKeysAll]),
    forbiddenPlayerKeys = StringList.FromRows([DataManager.FromList(random.m_altNotRequiredPlayerKeys)]),
    requiredKnownItems = DataManager.FromList(random.m_altRequiredKnownItems),
    forbiddenKnownItems = DataManager.FromList(random.m_altRequiredNotKnownItems),
    startMessage = random.m_startMessage,
    endMessage = random.m_endMessage,
    forceMusic = random.m_forceMusic,
    forceEnvironment = random.m_forceEnvironment,
    radius = random.m_eventRange,
    customChance = random.m_standaloneChance,
    customInterval = random.m_standaloneInterval,
    spawnerDelay = random.m_spawnerDelay,
  };

  internal static void ResolveEventConfiguration(RandEventSystem __instance, ref RandomEvent ev)
  {
    ev = __instance.GetEvent(ev?.m_name);
  }
}