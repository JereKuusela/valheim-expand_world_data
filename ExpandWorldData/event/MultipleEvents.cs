using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorld.Event;

public class MultiEvent(RandomEvent ev, int count)
{
  public RandomEvent Event = ev;
  public int Count = count;
}

[HarmonyPatch(typeof(RandEventSystem))]
public class MultipleEvents
{
  public static readonly List<MultiEvent> Events = [];

  [HarmonyPatch(nameof(RandEventSystem.FixedUpdate)), HarmonyPrefix]
  static bool FixedUpdate(RandEventSystem __instance)
  {
    if (!Configuration.DataEvents || Helper.IsClient() || !Configuration.MultipleEvents) return true;
    var delta = Time.fixedDeltaTime;
    __instance.UpdateForcedEvents(delta);
    __instance.UpdateRandomEvent(delta);
    __instance.m_forcedEvent?.Update(true, true, true, delta);
    var expired = Events.Where(entry => entry.Event.Update(true, true, __instance.IsAnyPlayerInEventArea(entry.Event), delta)).ToList();
    expired.ForEach(entry => entry.Event.OnStop());
    Events.RemoveAll(expired.Contains);
    if (__instance.m_forcedEvent != null) __instance.SetActiveEvent(__instance.m_forcedEvent);
    else if (Player.m_localPlayer)
    {
      var randomEvent = Events.OrderBy(entry => Utils.DistanceXZ(entry.Event.m_pos, Player.m_localPlayer.transform.position)).FirstOrDefault()?.Event;
      __instance.m_randomEvent = randomEvent;
      __instance.SetActiveEvent(randomEvent != null && __instance.IsInsideRandomEventArea(randomEvent, Player.m_localPlayer.transform.position) ? randomEvent : null);
    }
    else __instance.SetActiveEvent(null);
    return false;
  }

  [HarmonyPatch(nameof(RandEventSystem.SetRandomEvent)), HarmonyPrefix]
  static bool SetRandomEvent(RandEventSystem __instance, RandomEvent ev, Vector3 pos)
  {
    if (!Configuration.DataEvents || Helper.IsClient() || !Configuration.MultipleEvents) return true;
    if (ev == null)
    {
      var toStop = Events.ToList();
      toStop.ForEach(entry => entry.Event.OnStop());
      Events.RemoveAll(toStop.Contains);
      return false;
    }
    var nearby = Events.Where(entry => Utils.DistanceXZ(entry.Event.m_pos, pos) < Configuration.EventMinimumDistance).ToList();
    nearby.ForEach(entry => entry.Event.OnStop());
    Events.RemoveAll(nearby.Contains);
    var randomEvent = ev.Clone();
    randomEvent.m_pos = pos;
    randomEvent.OnStart();
    Events.Add(new(randomEvent, nearby.Sum(entry => entry.Count) + 1));
    __instance.SendCurrentRandomEvent();
    return false;
  }

  [HarmonyPatch(nameof(RandEventSystem.SendCurrentRandomEvent)), HarmonyPrefix]
  static bool SendCurrentRandomEvent()
  {
    if (!Configuration.DataEvents || Helper.IsClient() || !Configuration.MultipleEvents || Events.Count == 0) return true;
    if (Events.Count == 1)
    {
      var randomEvent = Events[0].Event;
      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SetEvent", [randomEvent.m_name, randomEvent.m_time, randomEvent.m_pos]);
      return false;
    }
    ZNet.instance.GetPeers().ForEach(peer =>
    {
      if (peer.m_rpc == null) return;
      var randomEvent = Events.OrderBy(entry => Utils.DistanceXZ(entry.Event.m_pos, peer.m_refPos)).First().Event;
      ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "SetEvent", [randomEvent.m_name, randomEvent.m_time, randomEvent.m_pos]);
    });
    return false;
  }
}