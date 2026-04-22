using System;
using System.Collections.Generic;
using Service;
using UnityEngine;
using Data;
using System.Linq;
namespace ExpandWorldData;

public class LocationExtra
{
  public static Dictionary<ZoneSystem.ZoneLocation, LocationExtraInfo> ExtraInfo = [];

  public static void AddInfo(ZoneSystem.ZoneLocation loc, LocationYaml data, string fileName)
  {
    var extra = new LocationExtraInfo(data, fileName);
    ExtraInfo[loc] = extra;
  }

  public static void ClearInfo()
  {
    ExtraInfo.Clear();
  }

  public static HashSet<ZoneSystem.ZoneLocation> GetNoBuilds()
  {
    return ExtraInfo.Where(kvp => !string.IsNullOrEmpty(kvp.Value.Data.noBuild) || !string.IsNullOrEmpty(kvp.Value.Data.noBuildDungeon)).Select(kvp => kvp.Key).ToHashSet();
  }

  public static List<Tuple<string, float>>? GetGroups(ZoneSystem.ZoneLocation? loc, bool maxGroup)
  {
    if (TryGet(loc, out var extra))
    {
      var groups = maxGroup ? extra.GroupsMax : extra.Groups;
      if (groups != null && groups.Count > 0)
        return groups;
    }
    return null;
  }

  private static DataEntry? ResolveData(LocationExtraInfo extra, string prefab, bool dungeon)
  {
    var objectData = dungeon ? extra.DungeonObjectData : extra.ObjectData;
    if (objectData == null) return null;
    return Spawn.GetData(objectData, prefab);
  }

  private static string ResolvePrefabOverride(LocationExtraInfo extra, string prefab, bool dungeon)
  {
    var objectSwaps = dungeon ? extra.DungeonObjectSwaps : extra.ObjectSwaps;
    if (objectSwaps == null) return prefab;
    if (!objectSwaps.TryGetValue(prefab, out var swaps)) return prefab;
    return Spawn.RandomizeSwap(swaps);
  }

  private static void ExecuteCommands(LocationExtraInfo extra, Vector3 pos, Quaternion rot)
  {
    if (extra.Commands == null) return;
    CommandManager.Run(extra.Commands, pos, rot.eulerAngles);
  }

  private static Vector3 ResolveScale(LocationExtraInfo extra)
  {
    if (extra.Scale == null) return Vector3.one;
    return Helper.RandomValue(extra.Scale);
  }

  public static bool TryGet(ZoneSystem.ZoneLocation? location, out LocationExtraInfo extra)
  {
    if (location == null)
    {
      extra = null!;
      return false;
    }
    return ExtraInfo.TryGetValue(location, out extra);
  }

  public static DataEntry? MergeData(ZoneSystem.ZoneLocation? location, DataEntry? pkg, string prefab, bool dungeon = false)
  {
    if (!TryGet(location, out var extra)) return pkg;
    var data = ResolveData(extra, prefab, dungeon);
    return DataHelper.Merge(data, pkg);
  }

  public static DataEntry? GetData(ZoneSystem.ZoneLocation? location, string prefab, bool dungeon = false)
  {
    if (!TryGet(location, out var extra)) return null;
    return ResolveData(extra, prefab, dungeon);
  }

  public static string GetPrefabOverride(ZoneSystem.ZoneLocation? location, string prefab, bool dungeon = false)
  {
    if (!TryGet(location, out var extra)) return prefab;
    return ResolvePrefabOverride(extra, prefab, dungeon);
  }

  public static void RunCommand(ZoneSystem.ZoneLocation? location, Vector3 pos, Quaternion rot)
  {
    if (!TryGet(location, out var extra)) return;
    ExecuteCommands(extra, pos, rot);
  }

  public static Vector3 GetScale(ZoneSystem.ZoneLocation? location)
  {
    if (!TryGet(location, out var extra)) return Vector3.one;
    return ResolveScale(extra);
  }

  public static string GetDungeonName(ZoneSystem.ZoneLocation? location, DungeonGenerator dg)
  {
    var dungeonName = Utils.GetPrefabName(dg.gameObject) ?? "";
    if (!TryGet(location, out var extra)) return dungeonName;
    if (extra.Dungeon == null || extra.Dungeon == "") return dungeonName;
    return extra.Dungeon;
  }

  public static bool TryGetData(ZoneSystem.ZoneLocation? location, out LocationYaml data)
  {
    if (!TryGet(location, out var extra))
    {
      data = new();
      return false;
    }
    data = extra.Data;
    return true;
  }

}
public class LocationExtraInfo
{
  public string? ZDOData;
  public string? Dungeon;
  public List<Tuple<string, float>>? Groups;
  public List<Tuple<string, float>>? GroupsMax;
  public Dictionary<string, List<Tuple<float, string>>>? ObjectSwaps;
  public Dictionary<string, List<Tuple<float, string>>>? DungeonObjectSwaps;
  public Dictionary<string, List<Tuple<float, DataEntry?>>>? ObjectData;
  public Dictionary<string, List<Tuple<float, DataEntry?>>>? DungeonObjectData;
  public List<BlueprintObject>? Objects;
  public Range<Vector3>? Scale;
  public string[]? Commands;
  public LocationYaml Data;

  public LocationExtraInfo(LocationYaml data, string fileName)
  {
    Data = data;
    LoadGroups(data);
    if (data.data != "")
      ZDOData = data.data;
    if (data.dungeon != "")
      Dungeon = data.dungeon;
    LoadObjectData(data, fileName);
    LoadObjectSwaps(data);
    if (data.objects != null)
      Objects = Helper.ParseObjects(data.objects, fileName);
    if (data.commands != null)
      Commands = data.commands;

    Range<Vector3> scale = new(Parse.Scale(data.scaleMin), Parse.Scale(data.scaleMax))
    {
      Uniform = data.scaleUniform
    };
    if (scale.Min != scale.Max)
      Scale = scale;
  }

  private static float? ParseFirstDistance(string[]? groups)
  {
    if (groups == null) return null;
    foreach (var entry in groups)
    {
      var kvp = Parse.Kvp(entry);
      if (string.IsNullOrEmpty(kvp.Key)) continue;
      if (string.IsNullOrEmpty(kvp.Value)) continue;
      return Parse.Float(kvp.Value);
    }
    return null;
  }

  private static List<Tuple<string, float>>? ParseGroups(string[]? groups, float defaultDistance)
  {
    if (groups == null) return null;
    var parsed = new List<Tuple<string, float>>();
    foreach (var entry in groups)
    {
      var kvp = Parse.Kvp(entry);
      var group = kvp.Key.Trim();
      if (group == "") continue;
      var distance = kvp.Value == "" ? defaultDistance : Parse.Float(kvp.Value, defaultDistance);
      parsed.Add(new(group, distance));
    }
    if (parsed.Count == 0) return null;
    return parsed;
  }

  private static void AddGroup(List<Tuple<string, float>> groups, string group, float distance)
  {
    if (group == "") return;
    if (groups.Any(g => g.Item1 == group)) return;
    groups.Add(new(group, distance));
  }

  private void LoadGroups(LocationYaml data)
  {
    var defaultMin = data.minDistanceFromSimilar;
    if (defaultMin == 0f)
      defaultMin = ParseFirstDistance(data.groups) ?? 0f;
    if (data.minDistanceFromSimilar == 0f)
      data.minDistanceFromSimilar = defaultMin;

    var defaultMax = data.maxDistanceFromSimilar;
    if (defaultMax == 0f)
      defaultMax = ParseFirstDistance(data.groupsMax) ?? 0f;
    if (data.maxDistanceFromSimilar == 0f)
      data.maxDistanceFromSimilar = defaultMax;

    var groups = ParseGroups(data.groups, defaultMin) ?? [];
    var groupsMax = ParseGroups(data.groupsMax, defaultMax) ?? [];

    AddGroup(groups, data.group, defaultMin);
    AddGroup(groupsMax, data.groupMax, defaultMax);

    if (data.group == "" && groups.Count > 0)
      data.group = groups[0].Item1;
    if (data.groupMax == "" && groupsMax.Count > 0)
      data.groupMax = groupsMax[0].Item1;

    Groups = groups.Count == 0 ? null : groups;
    GroupsMax = groupsMax.Count == 0 ? null : groupsMax;
  }

  private void LoadObjectData(LocationYaml data, string fileName)
  {
    Dictionary<string, List<Tuple<float, DataEntry?>>>? locationobjectData = null;
    Dictionary<string, List<Tuple<float, DataEntry?>>>? dungeonobjectData = null;

    if (data.objectData != null)
    {
      locationobjectData = Spawn.LoadData(data.objectData, fileName);
      dungeonobjectData = Spawn.LoadData(data.objectData, fileName);
    }
    if (data.locationObjectData != null)
    {
      var objectData = Spawn.LoadData(data.locationObjectData, fileName);
      if (locationobjectData == null)
      {
        locationobjectData = objectData;
      }
      else
      {
        foreach (var kvp in objectData)
          locationobjectData[kvp.Key] = kvp.Value;
      }
    }
    if (data.dungeonObjectData != null)
    {
      var objectData = Spawn.LoadData(data.dungeonObjectData, fileName);
      if (dungeonobjectData == null)
      {
        dungeonobjectData = objectData;
      }
      else
      {
        foreach (var kvp in objectData)
          dungeonobjectData[kvp.Key] = kvp.Value;
      }
    }
    if (dungeonobjectData != null)
      DungeonObjectData = dungeonobjectData;
    if (locationobjectData != null)
      ObjectData = locationobjectData;
  }

  private void LoadObjectSwaps(LocationYaml data)
  {
    Dictionary<string, List<Tuple<float, string>>>? locationobjectSwaps = null;
    Dictionary<string, List<Tuple<float, string>>>? dungeonobjectSwaps = null;

    if (data.objectSwap != null)
    {
      locationobjectSwaps = Spawn.LoadSwaps(data.objectSwap);
      dungeonobjectSwaps = Spawn.LoadSwaps(data.objectSwap);
    }
    if (data.locationObjectSwap != null)
    {
      var objectSwap = Spawn.LoadSwaps(data.locationObjectSwap);
      if (locationobjectSwaps == null)
      {
        locationobjectSwaps = objectSwap;
      }
      else
      {
        foreach (var kvp in objectSwap)
          locationobjectSwaps[kvp.Key] = kvp.Value;
      }
    }
    if (data.dungeonObjectSwap != null)
    {
      var objectSwap = Spawn.LoadSwaps(data.dungeonObjectSwap);
      if (dungeonobjectSwaps == null)
      {
        dungeonobjectSwaps = objectSwap;
      }
      else
      {
        foreach (var kvp in objectSwap)
          dungeonobjectSwaps[kvp.Key] = kvp.Value;
      }
    }
    if (dungeonobjectSwaps != null)
      DungeonObjectSwaps = dungeonobjectSwaps;
    if (locationobjectSwaps != null)
      ObjectSwaps = locationobjectSwaps;
  }

}
