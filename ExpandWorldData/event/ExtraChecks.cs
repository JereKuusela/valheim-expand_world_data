using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using Service;
using UnityEngine;
using Common;

namespace ExpandWorld.Event;

public class ExtraChecks
{
  internal static bool ValidateEventRequirements(bool result, RandomEvent ev, Vector3 point)
  {
    if (!result || !Loader.ExtraData.TryGetValue(ev, out var data)) return result;
    return KeyCheck(data) && EnvCheck(point, data) && PlayerCheck(point, data.PlayerLimit, data.PlayerDistance) && EventCheck(point, data.EventLimit);
  }

  private static bool KeyCheck(ExtraData data) =>
    (data.RequiredKeyRows == null || StringList.Pass(data.RequiredKeyRows, ZoneSystem.instance.GetGlobalKey))
    && !StringList.Forbids(data.ForbiddenKeyRows, ZoneSystem.instance.GetGlobalKey);

  private static bool EnvCheck(Vector3 position, ExtraData data)
  {
    var required = data.RequiredEnvironments;
    var forbidden = data.ForbiddenEnvironments;
    if (required == null && forbidden == null) return true;
    var environments = EnvMan.instance.GetAvailableEnvironments(WorldGenerator.instance.GetBiomeSector(position));
    if (environments == null || environments.Count == 0) return required == null;
    var state = Random.state;
    Random.InitState((int)((long)ZNet.instance.GetTimeSeconds() / EnvMan.instance.m_environmentDuration));
    var environment = EnvMan.instance.SelectWeightedEnvironment(environments);
    Random.state = state;
    var name = environment.m_name.ToLower();
    bool Has(string value) => value == name;
    if (StringList.Forbids(forbidden, Has)) return false;
    return required == null || StringList.Pass(required, Has);
  }

  private static bool PlayerCheck(Vector3 position, ValueRange<int>? limit, float distance)
  {
    if (limit == null) return true;
    var count = RandEventSystem.s_playerEventDatas.Count(player => Utils.DistanceXZ(position, player.position) <= distance);
    return limit.Min <= count && count <= limit.Max;
  }

  private static bool EventCheck(Vector3 position, ValueRange<int>? limit)
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