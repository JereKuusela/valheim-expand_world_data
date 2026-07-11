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
  public static Dictionary<string, List<Tuple<string, float>>> ExtraInfoByVirtualId = [];

  public static void AddInfo(ZoneSystem.ZoneLocation loc, LocationYaml data, string fileName)
  {
    var extra = new LocationExtraInfo(data, fileName);
    // Distance rules may update these for compatibility.
    loc.m_minDistanceFromSimilar = data.minDistanceFromSimilar;
    loc.m_maxDistanceFromSimilar = data.maxDistanceFromSimilar;
    loc.m_group = data.group;
    loc.m_groupMax = data.groupMax;
    ExtraInfo[loc] = extra;
    AddVirtualRules(loc.m_group, extra.AwayFrom);
    AddVirtualRules(loc.m_groupMax, extra.CloseTo);
  }

  private static void AddVirtualRules(string group, List<Tuple<string, float>>? rules)
  {
    if (!IsVirtualGroupId(group)) return;
    if (rules == null || rules.Count == 0) return;
    ExtraInfoByVirtualId[group] = rules;
  }

  public static void ClearInfo()
  {
    ExtraInfo.Clear();
    ExtraInfoByVirtualId.Clear();
  }

  public static HashSet<ZoneSystem.ZoneLocation> GetNoBuilds()
  {
    return ExtraInfo.Where(kvp => !string.IsNullOrEmpty(kvp.Value.Data.noBuild) || !string.IsNullOrEmpty(kvp.Value.Data.noBuildDungeon)).Select(kvp => kvp.Key).ToHashSet();
  }

  public static bool IsVirtualGroupId(string group)
  {
    if (group.Length < 2) return false;
    if (group[0] != '_') return false;
    return int.TryParse(group.Substring(1), out _);
  }

  public static List<Tuple<string, float>>? GetDistanceRules(string group)
  {
    if (string.IsNullOrEmpty(group)) return null;
    if (!ExtraInfoByVirtualId.TryGetValue(group, out var rules)) return null;
    return rules;
  }

  public static bool MatchesTarget(ZoneSystem.ZoneLocation? location, string target)
  {
    if (location == null) return false;
    if (location.m_prefab.Name == target) return true;
    if (!TryGet(location, out var extra)) return false;
    if (extra.Groups == null) return false;
    return extra.Groups.Contains(target);
  }
  public static bool MatchesTarget(ZoneSystem.ZoneLocation? location, string prefabName, string group)
  {
    if (location == null) return false;
    if (location.m_prefab.Name == prefabName) return true;
    if (!TryGet(location, out var extra)) return false;
    if (extra.Groups == null) return false;
    return extra.Groups.Contains(group);
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
  private static int VirtualGroupId = 0;

  public string? ZDOData;
  public string? Dungeon;
  public HashSet<string> Groups;
  public List<Tuple<string, float>>? AwayFrom;
  public List<Tuple<string, float>>? CloseTo;
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
    Groups = ParseMembership(data.groups);
    if (data.group != "")
      Groups.Add(data.group);
    if (data.groupMax != "")
      Groups.Add(data.groupMax);

    LoadDistanceRules(data, fileName);
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

  private static List<Tuple<string, float>>? ParseDistanceRules(string[]? rules, string fieldName, string fileName)
  {
    if (rules == null) return null;
    var parsed = new List<Tuple<string, float>>();
    foreach (var entry in rules)
    {
      var kvp = Parse.Kvp(entry);
      var target = kvp.Key.Trim();
      if (target == "") continue;
      if (kvp.Value == "")
      {
        Log.Warning($"{fileName}: Invalid {fieldName} value '{entry}', expected 'target,distance'.");
        continue;
      }
      if (!Parse.TryFloat(kvp.Value, out var distance))
      {
        Log.Warning($"{fileName}: Invalid {fieldName} distance '{kvp.Value}' in value '{entry}'.");
        continue;
      }
      parsed.Add(new(target, distance));
    }
    if (parsed.Count == 0) return null;
    return parsed;
  }

  private static HashSet<string> ParseMembership(string groups)
  {
    if (groups == "") return [];
    return [.. Parse.Split(groups).Where(group => group != "")];
  }

  // Group check uses location prefab name which is not distinct, so group name is used instead for unique identifier.
  private static string CreateVirtualGroupName()
  {
    VirtualGroupId += 1;
    return $"_{VirtualGroupId}";
  }

  private void LoadDistanceRules(LocationYaml data, string fileName)
  {

    var awayFrom = ParseDistanceRules(data.awayFrom, nameof(data.awayFrom), fileName);
    var closeTo = ParseDistanceRules(data.closeTo, nameof(data.closeTo), fileName);

    if (awayFrom != null)
    {
      data.group = CreateVirtualGroupName();
      // Must be greater than zero to be considered.
      data.minDistanceFromSimilar = 1f;
      AwayFrom = awayFrom;
    }
    if (closeTo != null)
    {
      data.groupMax = CreateVirtualGroupName();
      // Must be greater than zero to be considered.
      data.maxDistanceFromSimilar = 1f;
      CloseTo = closeTo;
    }
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
