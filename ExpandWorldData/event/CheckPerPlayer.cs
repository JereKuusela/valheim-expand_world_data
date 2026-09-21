using System.Collections.Generic;
using ExpandWorldData;
using UnityEngine;

namespace ExpandWorld.Event;

public class CheckPerPlayer
{
  internal static void UpdateEvents(RandEventSystem __instance, float dt)
  {
    if (Helper.IsClient() || Game.m_eventRate == 0f) return;
    if (RandEventSystem.s_randomEventNeedsRefresh) RandEventSystem.RefreshPlayerEventData();
    CheckGlobalEvent(__instance, dt);
    CheckStandaloneEvents(__instance, dt);
  }

  private static void CheckGlobalEvent(RandEventSystem system, float delta)
  {
    if (system.m_eventTimer + delta <= system.m_eventIntervalMin * 60f * Game.m_eventRate) return;
    system.m_eventTimer = -delta;
    foreach (var player in RandEventSystem.s_playerEventDatas)
    {
      if (Random.Range(0f, 100f) > system.m_eventChance / Game.m_eventRate) continue;
      var events = GetPossibleRandomEvents(system, player);
      if (events.Count == 0) continue;
      var selected = events[Random.Range(0, events.Count)];
      system.SetRandomEvent(selected.Key, selected.Value);
    }
  }

  private static void CheckStandaloneEvents(RandEventSystem system, float delta)
  {
    foreach (var player in RandEventSystem.s_playerEventDatas)
    {
      List<RandEventSystem.PlayerEventData> players = [player];
      foreach (var randomEvent in system.m_events)
      {
        if (!randomEvent.m_enabled || randomEvent.m_standaloneChance == 0f || system.m_activeEvent == randomEvent) continue;
        if (randomEvent.m_time + delta <= randomEvent.m_standaloneInterval * Game.m_eventRate) continue;
        randomEvent.m_time = -delta;
        if (Random.Range(0f, 100f) > randomEvent.m_standaloneChance / Game.m_eventRate || !system.HaveGlobalKeys(randomEvent, players)) continue;
        var points = system.GetValidEventPoints(randomEvent, players);
        if (points.Count > 0) system.SetRandomEvent(randomEvent, points[Random.Range(0, points.Count)]);
      }
    }
  }

  private static List<KeyValuePair<RandomEvent, Vector3>> GetPossibleRandomEvents(RandEventSystem system, RandEventSystem.PlayerEventData player)
  {
    system.m_lastPossibleEvents.Clear();
    foreach (var randomEvent in system.m_events)
    {
      List<RandEventSystem.PlayerEventData> players = [player];
      if (!randomEvent.m_enabled || !randomEvent.m_random || !system.HaveGlobalKeys(randomEvent, players)) continue;
      var points = system.GetValidEventPoints(randomEvent, players);
      if (points.Count > 0) system.m_lastPossibleEvents.Add(new(randomEvent, points[Random.Range(0, points.Count)]));
    }
    return system.m_lastPossibleEvents;
  }
}