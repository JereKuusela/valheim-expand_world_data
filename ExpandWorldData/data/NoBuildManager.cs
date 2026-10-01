using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class NoBuildData
{
  public float X;
  public float Z;
  public float radius;
  public float dungeon;
}

public class NoBuildManager
{
  private static bool Initialized;
  private static bool Pending;

  /// <summary>Marks this manager ready to accept sync updates. Does not replay pending data yet
  /// — call <see cref="ApplyPending"/> once every manager has initialized.</summary>
  public static void Initialize()
  {
    Initialized = true;
  }

  public static void ApplyPending()
  {
    if (!Pending) return;
    Pending = false;
    Apply(Configuration.valueNoBuildData.Value);
  }

  public static void CleanUp()
  {
    Initialized = false;
    Pending = false;
    NoBuild.Clear();
  }

  public static void UpdateData()
  {
    var noBuilds = LocationExtra.GetNoBuilds();
    var locations = ZoneSystem.instance.m_locationInstances.Values.Where(loc => noBuilds.Contains(loc.m_location));
    var data = locations.Select(loc =>
    {
      var noBuild = "false";
      var noBuildDungeon = "false";
      if (LocationExtra.TryGetData(loc.m_location, out var locationData))
      {
        noBuild = locationData.noBuild;
        noBuildDungeon = locationData.noBuildDungeon;
      }

      var radius = noBuild == "true" ? loc.m_location.m_exteriorRadius : Parse.Float(noBuild);
      // Negative value means the whole zone.
      var dungeon = noBuildDungeon == "true" ? -1f : Parse.Float(noBuildDungeon);
      return new NoBuildData()
      {
        X = loc.m_position.x,
        Z = loc.m_position.z,
        radius = radius,
        dungeon = dungeon,
      };
    }).Where(x => x.radius != 0f || x.dungeon != 0f).ToList();
    Configuration.valueNoBuildData.Value = Yaml.Serializer().Serialize(data);
  }
  private static Dictionary<Vector2s, List<NoBuildData>> NoBuild = [];
  public static bool HasData => NoBuild.Count > 0;
  public static bool IsInsideNoBuildZone(Vector3 point)
  {
    var zone = ZoneSystem.GetZone(point);
    if (!NoBuild.TryGetValue(zone, out var candidates)) return false;
    foreach (var noBuild in candidates)
    {
      if (point.y <= 3000 && Utils.DistanceXZ(new(noBuild.X, 0, noBuild.Z), point) < noBuild.radius)
        return true;
      if (point.y > 3000 && noBuild.dungeon < 0f && zone == ZoneSystem.GetZone(new(noBuild.X, 0, noBuild.Z)))
        return true;
      if (point.y > 3000 && Utils.DistanceXZ(new(noBuild.X, 0, noBuild.Z), point) < noBuild.dungeon)
        return true;
    }
    return false;
  }
  private static Dictionary<Vector2s, List<NoBuildData>> CreateIndex(List<NoBuildData> data)
  {
    Dictionary<Vector2s, List<NoBuildData>> index = [];
    foreach (var noBuild in data)
    {
      if (float.IsNaN(noBuild.radius) || float.IsInfinity(noBuild.radius) ||
          float.IsNaN(noBuild.dungeon) || float.IsInfinity(noBuild.dungeon) ||
          float.IsNaN(noBuild.X) || float.IsInfinity(noBuild.X) ||
          float.IsNaN(noBuild.Z) || float.IsInfinity(noBuild.Z))
        throw new FormatException("No build coordinates and radii must be finite.");
      if (noBuild.radius <= 0f && noBuild.dungeon == 0f) continue;
      var radius = Mathf.Max(0f, Mathf.Max(noBuild.radius, noBuild.dungeon));
      var min = ZoneSystem.GetZone(new(noBuild.X - radius, 0, noBuild.Z - radius));
      var max = ZoneSystem.GetZone(new(noBuild.X + radius, 0, noBuild.Z + radius));
      for (var zoneX = (int)min.x; zoneX <= max.x; ++zoneX)
      {
        for (var zoneZ = (int)min.y; zoneZ <= max.y; ++zoneZ)
        {
          Vector2s zone = new(zoneX, zoneZ);
          if (!index.TryGetValue(zone, out var candidates)) index[zone] = candidates = [];
          candidates.Add(noBuild);
        }
      }
    }
    return index;
  }
  public static bool IsInsideNoBuildBiome(Vector3 point)
  {
    var biome = WorldGenerator.instance.GetBiome(point);
    return (biome & BiomeManager.NoBuildBiomes) != 0;
  }
  public static bool IsInsideNoBuildTerritory(Vector3 point)
  {
    var territory = BiomeCalculator.GetTerritory(point.x, point.z);
    return territory != null && territory.noBuild;
  }
  public static void Apply(string yaml)
  {
    if (Helper.IsClient() && !Initialized)
    {
      Pending = true;
      return;
    }
    try
    {
      var data = string.IsNullOrEmpty(yaml) ? new List<NoBuildData>() : Yaml.Deserialize<NoBuildData>(yaml, "No build");
      NoBuild = CreateIndex(data);
      Log.Info($"Reloading no build data ({data.Count} entries).");
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    finally { Refresh.Patches(); }
  }

  internal static bool CheckAdditionalZones(bool result, Vector3 point)
  {
    return result ||
           IsInsideNoBuildZone(point) ||
           IsInsideNoBuildTerritory(point) ||
           IsInsideNoBuildBiome(point);
  }

  internal static void SynchronizeLocationData() => UpdateData();

  internal static void SynchronizeGeneratedLocations(bool value)
  {
    if (value) UpdateData();
  }
}