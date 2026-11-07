using System.Collections.Generic;
using System.Linq;
using Data;
using ExpandWorldData.Data;
using Service;
using UnityEngine;

namespace ExpandWorldData.PersistentEvents;

public static class PersistentEventRuntime
{
  // Events the commands and messages were already handled for.
  private static readonly Dictionary<int, PersistentEventSystem.ActivePersistentEvent> Known = [];
  private static readonly Dictionary<PersistentEventSystem.PersistentEvent, double> NextRoll = [];

  public static void Reset(PersistentEventSystem system)
  {
    Known.Clear();
    NextRoll.Clear();
    if (Helper.IsServer())
      foreach (var ev in system.m_activePersistentEvents.list) Known[ev.eventId] = ev;
  }

  private static bool IsValid(PersistentEventSystem system, int index) => index >= 0 && index < system.m_possibleEvents.Count;

  // Saved and synced events refer to the list by index, so removed entries would break the game.
  private static void DropInvalid(PersistentEventSystem system)
  {
    var removed = system.m_activePersistentEvents.list.RemoveAll(ev => !IsValid(system, ev.sourceEventId));
    if (removed > 0) Log.Warning($"Removed {removed} active persistent events with a missing definition.");
  }

  internal static void Awake()
  {
    Known.Clear();
    NextRoll.Clear();
  }

  internal static void AfterLoad(PersistentEventSystem __instance)
  {
    DropInvalid(__instance);
    Reset(__instance);
  }

  internal static void AfterClientUpdate(PersistentEventSystem __instance) => DropInvalid(__instance);

  internal static bool BeforeStart(PersistentEventSystem __instance, int sourceEventId)
  {
    if (!IsValid(__instance, sourceEventId)) return false;
    return !PersistentEventLoader.Extra.TryGetValue(__instance.m_possibleEvents[sourceEventId], out var extra) || extra.Enabled;
  }

  internal static void AfterStart(PersistentEventSystem __instance, int sourceEventId)
  {
    if (!IsValid(__instance, sourceEventId)) return;
    var list = __instance.m_activePersistentEvents.list;
    if (list.Count == 0) return;
    var active = list[list.Count - 1];
    if (active.sourceEventId != sourceEventId || active.eventId != __instance.m_eventIdCounter - 1 || active.duration >= 0.0) return;
    var source = __instance.m_possibleEvents[sourceEventId];
    if (!source.hasDuration) return;
    active.startTime = ZNet.instance.GetTimeSeconds();
    active.duration = Random.Range(source.minDurationInSeconds, source.maxDurationInSeconds);
  }

  internal static void AfterListUpdate(PersistentEventSystem __instance, long target)
  {
    // Updates for a single joining peer don't change the list.
    if (target != 0) return;
    var current = __instance.m_activePersistentEvents.list;
    foreach (var ev in current.ToList())
    {
      if (Known.ContainsKey(ev.eventId)) continue;
      Known[ev.eventId] = ev;
      Notify(__instance, ev, true);
    }
    var ids = current.Select(ev => ev.eventId).ToHashSet();
    foreach (var pair in Known.Where(pair => !ids.Contains(pair.Key)).ToList())
    {
      Known.Remove(pair.Key);
      Notify(__instance, pair.Value, false);
    }
  }

  private static void Notify(PersistentEventSystem system, PersistentEventSystem.ActivePersistentEvent ev, bool start)
  {
    if (!IsValid(system, ev.sourceEventId)) return;
    if (!PersistentEventLoader.Extra.TryGetValue(system.m_possibleEvents[ev.sourceEventId], out var extra)) return;
    var message = start ? extra.StartMessage : extra.EndMessage;
    var commands = start ? extra.StartCommands : extra.EndCommands;
    if (message != "")
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", (int)MessageHud.MessageType.Center, message);
    if (commands != null)
      CommandManager.RunCommand(commands, ev.position, Vector3.zero);
  }

  internal static void RollRandomEvents(PersistentEventSystem __instance)
  {
    if (!Helper.IsServer() || !ZoneSystem.instance) return;
    var now = Time.timeAsDouble;
    for (var index = 0; index < __instance.m_possibleEvents.Count; index++)
    {
      var ev = __instance.m_possibleEvents[index];
      if (!PersistentEventLoader.Extra.TryGetValue(ev, out var extra) || !extra.Enabled || extra.Interval <= 0f) continue;
      if (!NextRoll.TryGetValue(ev, out var next))
      {
        NextRoll[ev] = now + extra.Interval;
        continue;
      }
      if (now < next) continue;
      NextRoll[ev] = now + extra.Interval;
      if (Random.value * 100f >= extra.Chance) continue;
      if (extra.RequiredKeys != null && !StringList.Pass(extra.RequiredKeys, ZoneSystem.instance.GetGlobalKey)) continue;
      if (StringList.Forbids(extra.ForbiddenKeys, ZoneSystem.instance.GetGlobalKey)) continue;
      __instance.RPC_RequestStartEvent(0L, index);
    }
  }

  internal static bool CreateObjects(PersistentEventSystem.ObjectSpawnSettings __instance, PersistentEventSystem.ActivePersistentEvent sourceEvent, List<Vector3>? spawnedObjectPositions)
  {
    if (!PersistentEventLoader.ObjectExtra.TryGetValue(__instance, out var extra) || extra.Prefabs.Length == 0) return true;
    spawnedObjectPositions ??= [];
    var count = Random.Range(__instance.minCount, Mathf.Max(__instance.maxCount, __instance.minCount));
    var maxSlope = __instance.maxSlope / 90f;
    var minDistanceSqr = __instance.minDistanceFromOtherEventObjects * __instance.minDistanceFromOtherEventObjects;
    var radius = __instance.maxDistanceFromEventCenter <= 0.01f ? sourceEvent.radius : __instance.maxDistanceFromEventCenter;
    var water = Helper.AltitudeToHeight(0f);
    for (var i = 0; i < count; i++)
    {
      var attempts = __instance.important ? 100 : 1;
      for (var attempt = 0; attempt < attempts; attempt++)
      {
        var offset = Random.insideUnitCircle * radius;
        var pos = sourceEvent.position + new Vector3(offset.x, 0f, offset.y);
        if (__instance.maxSlope < 90f && 1f - Vector3.Dot(WorldGenerator.instance.GetNormal(pos.x, pos.z), Vector3.up) > maxSlope) continue;
        pos.y = WorldGenerator.instance.GetHeight(pos.x, pos.z);
        var rule = __instance.waterSpawning;
        if (rule == PersistentEventSystem.ObjectSpawnSettings.WaterSpawnRule.OnlySpawnInWater && pos.y > water - 1f) continue;
        if (rule == PersistentEventSystem.ObjectSpawnSettings.WaterSpawnRule.CanNotSpawnInWater && pos.y <= water) continue;
        pos.y = Mathf.Max(pos.y, water);
        if (minDistanceSqr > 0f && spawnedObjectPositions.Any(other => (other - pos).sqrMagnitude < minDistanceSqr)) continue;
        spawnedObjectPositions.Add(pos);
        var rot = __instance.randomRotation ? Quaternion.Euler(Vector3.up * Random.value * 360f) : Quaternion.identity;
        var prefab = extra.Prefabs[Random.Range(0, extra.Prefabs.Length)];
        BlueprintObject obj = new(prefab, Vector3.zero, Quaternion.identity, Vector3.one, extra.Data, 1f);
        Create.BPO(obj, pos, rot, Vector3.one, (data, _) => data, name => name, null);
        break;
      }
    }
    return false;
  }
}
