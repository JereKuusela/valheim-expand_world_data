using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Event;

public class ExtraChecks
{
  internal static bool ValidateEventRequirements(bool result, RandomEvent ev, Vector3 point)
  {
    if (!result || !Loader.ExtraData.TryGetValue(ev, out var data)) return result;
    return EnvCheck(point, data.RequiredEnvironments) && PlayerCheck(point, data.PlayerLimit, data.PlayerDistance) && EventCheck(point, data.EventLimit);
  }

  private static bool EnvCheck(Vector3 position, List<string> required)
  {
    if (required.Count == 0) return true;
    var environments = EnvMan.instance.GetAvailableEnvironments(WorldGenerator.instance.GetBiomeSector(position));
    if (environments == null || environments.Count == 0) return false;
    var state = Random.state;
    Random.InitState((int)((long)ZNet.instance.GetTimeSeconds() / EnvMan.instance.m_environmentDuration));
    var environment = EnvMan.instance.SelectWeightedEnvironment(environments);
    Random.state = state;
    return required.Contains(environment.m_name.ToLower());
  }

  private static bool PlayerCheck(Vector3 position, Range<int>? limit, float distance)
  {
    if (limit == null) return true;
    var count = RandEventSystem.s_playerEventDatas.Count(player => Utils.DistanceXZ(position, player.position) <= distance);
    return limit.Min <= count && count <= limit.Max;
  }

  private static bool EventCheck(Vector3 position, Range<int>? limit)
  {
    if (limit == null || !Configuration.MultipleEvents) return true;
    var count = MultipleEvents.Events.Where(entry => Utils.DistanceXZ(position, entry.Event.m_pos) <= Configuration.EventMinimumDistance).Sum(entry => entry.Count);
    return limit.Min <= count && count <= limit.Max;
  }

  internal static bool ValidateBaseValue(RandomEvent ev, RandEventSystem.PlayerEventData player, ref bool __result)
  {
    if (!Loader.ExtraData.TryGetValue(ev, out var data)) return true;
    __result = player.baseValue >= data.MinBaseValue && player.baseValue <= data.MaxBaseValue;
    return false;
  }
}