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
    public static Dictionary<string, LocationExtraInfo> ExtraInfoByGroup = [];

    public static void AddInfo(ZoneSystem.ZoneLocation loc, LocationYaml data, string fileName)
    {
        var extra = new LocationExtraInfo(data, fileName);
        // Groups might update these.
        loc.m_minDistanceFromSimilar = data.minDistanceFromSimilar;
        loc.m_maxDistanceFromSimilar = data.maxDistanceFromSimilar;
        loc.m_group = data.group;
        loc.m_groupMax = data.groupMax;
        ExtraInfo[loc] = extra;
        // Register both groups both group keys (m_group and m_groupMax) keys (when min and max differ) so the max-group lookup resolves.
        // Previously only m_group was registered, which keyed an empty string for ungrouped locations
        // and never registered m_groupMax at all, so max-group resolution kind of silently fell through. - Nick
        if (!string.IsNullOrEmpty(loc.m_group))
            ExtraInfoByGroup[loc.m_group] = extra;
        if (!string.IsNullOrEmpty(loc.m_groupMax) && loc.m_groupMax != loc.m_group)
            ExtraInfoByGroup[loc.m_groupMax] = extra;
    }

    public static void ClearInfo()
    {
        ExtraInfo.Clear();
        ExtraInfoByGroup.Clear();
    }

    public static HashSet<ZoneSystem.ZoneLocation> GetNoBuilds()
    {
        return ExtraInfo.Where(kvp => !string.IsNullOrEmpty(kvp.Value.Data.noBuild) || !string.IsNullOrEmpty(kvp.Value.Data.noBuildDungeon)).Select(kvp => kvp.Key).ToHashSet();
    }

    public static List<Tuple<string, float>>? GetGroups(ZoneSystem.ZoneLocation? loc, bool maxGroup)
    {
        if (!TryGet(loc, out var extra))
            return null;
        return maxGroup ? extra.GroupsMax : extra.Groups;
    }

    public static List<Tuple<string, float>>? GetGroups(string group, bool maxGroup)
    {
        if (string.IsNullOrEmpty(group)) return null;
        if (!ExtraInfoByGroup.TryGetValue(group, out var extra)) return null;
        return maxGroup ? extra.GroupsMax : extra.Groups;
    }

    /**
     Read-side accessor for a location's search-only anchor groups: the (group, distance) pairs it must spawn NEAR
     without advertising into them. LPA reads this through Api.GetAnchorGroups and unions it with the advertise set
     (GroupsMax) to build its max-similarity search set. Null for any location that declares no anchors, which collapses
     the search set back onto the advertise set - i.e. plain symmetric groupMax behaviour. -Nick
    */
    public static List<Tuple<string, float>>? GetAnchors(ZoneSystem.ZoneLocation? loc)
    {
        if (!TryGet(loc, out var extra))
            return null;
        return extra.Anchors;
    }

    /**
     Source-side resolution for the max-similarity check: the set a PLACING location searches for, i.e. its advertise
     groups (GroupsMax) plus its search-only anchors. The target side of the check still uses GetGroups, so an existing
     instance only ever matches on what it ADVERTISES. That asymmetry is the whole point - a satellite finds a host, but
     two satellites never find each other. With no anchors this returns GroupsMax unchanged, so a symmetric-groupMax
     world behaves exactly as before. -Nick
    */
    public static List<Tuple<string, float>>? GetSearchGroups(string group)
    {
        if (string.IsNullOrEmpty(group)) return null;
        if (!ExtraInfoByGroup.TryGetValue(group, out var extra)) return null;
        var max = extra.GroupsMax;
        var anchors = extra.Anchors;
        if (anchors == null || anchors.Count == 0) return max;
        if (max == null || max.Count == 0) return anchors;
        var combined = new List<Tuple<string, float>>(max);
        foreach (var a in anchors)
            if (!combined.Any(g => g.Item1 == a.Item1)) combined.Add(a);
        return combined;
    }

    /**
     True when a location searches anchors but advertises nothing (empty GroupsMax). The max-similarity check has a
     same-prefab shortcut ("another instance of me is nearby, good enough") which is correct for symmetric groupMax but
     wrong for a directed satellite: siblings share a prefab, so the shortcut would let them satisfy each other and
     drift off their hosts. The InRange patch consults this to suppress that shortcut for search-only types and force
     satisfaction to come from a real advertiser. -Nick
    */
    public static bool IsSearchOnly(string group)
    {
        if (string.IsNullOrEmpty(group)) return false;
        if (!ExtraInfoByGroup.TryGetValue(group, out var extra)) return false;
        var hasAnchors = extra.Anchors != null && extra.Anchors.Count > 0;
        var hasAdvertise = extra.GroupsMax != null && extra.GroupsMax.Count > 0;
        return hasAnchors && !hasAdvertise;
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
    public List<Tuple<string, float>>? Groups;
    public List<Tuple<string, float>>? GroupsMax;
    public List<Tuple<string, float>>? Anchors;
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

    // Group check uses location prefab name which is not distinct, so group name is used instead for unique identifier.
    private static string CreateVirtualGroupName()
    {
        VirtualGroupId += 1;
        return $"_{VirtualGroupId}";
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
            defaultMax = ParseFirstDistance(data.groupsMax) ?? ParseFirstDistance(data.anchors) ?? 0f;
        if (data.maxDistanceFromSimilar == 0f)
            data.maxDistanceFromSimilar = defaultMax;

        var groups = ParseGroups(data.groups, defaultMin) ?? [];
        var groupsMax = ParseGroups(data.groupsMax, defaultMax) ?? [];
        var anchors = ParseGroups(data.anchors, defaultMax) ?? [];



        AddGroup(groups, data.group, defaultMin);
        AddGroup(groupsMax, data.groupMax, defaultMax);

        if (groups.Count == 0 && groupsMax.Count == 0 && anchors.Count == 0) return;

        /**
         Only virtualize when there are actually multiple groups. A single group keeps its real name so that
         anything reading m_group sees a real, shared group instead of an per-entry handle. For convenience :)
         The virtual handle is only needed to disambiguate an entry that belongs to several groups, so
         using it for the single-group case broke same-group spacing for any consumer (which would be LPA) that keys on
         m_group directly (makes one real group out into N unique per-entry buckets :s). -Nick
        */
        string? virtualGroup = null;
        if (groups.Count > 1 || groupsMax.Count > 1)
            virtualGroup = CreateVirtualGroupName();

        if (groups.Count > 1)
        {
            data.group = virtualGroup;
            Groups = groups;
        }
        else if (groups.Count == 1)
        {
            data.group = groups[0].Item1;
            Groups = groups;
        }

        if (groupsMax.Count > 1)
        {
            data.groupMax = virtualGroup;
            GroupsMax = groupsMax;
        }
        else if (groupsMax.Count == 1)
        {
            data.groupMax = groupsMax[0].Item1;
            GroupsMax = groupsMax;
        }

        /**
         Search-only anchors: the location must spawn within range of a host that advertises the anchor group, but it
         never advertises the group itself (I deliberately do NOT add it to groupsMax), so satellites orbit hosts without
         seeding each other. maxDistanceFromSimilar is already primed from the first anchor distance above, which is what
         makes vanilla actually run the max-similarity check. If the location has no real groupMax to be looked up by, I
         mint a unique virtual handle: both vanilla and my InRange identify the placing location by its m_groupMax string,
         and a unique handle makes GetSearchGroups resolve to THIS location's anchors instead of colliding on a shared
         name. It advertises nothing regardless, since GroupsMax stays empty. -Nick
        */
        if (anchors.Count > 0)
        {
            Anchors = anchors;
            if (string.IsNullOrEmpty(data.groupMax))
                data.groupMax = CreateVirtualGroupName();
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