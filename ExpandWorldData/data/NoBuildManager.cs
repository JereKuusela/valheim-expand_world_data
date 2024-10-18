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

  public static void UpdateData()
  {
    if (ZoneSystem.instance.m_locationInstances.Count == 0) return;
    var noBuilds = LocationLoading.LocationData.Where(kvp => !string.IsNullOrEmpty(kvp.Value.noBuild) || !string.IsNullOrEmpty(kvp.Value.noBuildDungeon)).Select(kvp => kvp.Key).ToHashSet();
    var locations = ZoneSystem.instance.m_locationInstances.Values.Where(loc => noBuilds.Contains(loc.m_location.m_prefab.Name));
    var data = locations.Select(loc =>
    {
      var noBuild = "false";
      var noBuildDungeon = "false";
      if (LocationLoading.LocationData.TryGetValue(loc.m_location.m_prefab.Name, out var locationData))
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
  private static Dictionary<Vector2i, NoBuildData> NoBuild = [];
  public static bool IsInsideNoBuildZone(Vector3 point)
  {
    var zone = ZoneSystem.GetZone(point);
    for (var i = zone.x - 1; i <= zone.x + 1; ++i)
    {
      for (var j = zone.y - 1; j <= zone.y + 1; j++)
      {
        if (!NoBuild.TryGetValue(new(i, j), out var noBuild)) continue;
        if (point.y <= 3000 && Utils.DistanceXZ(new(noBuild.X, 0, noBuild.Z), point) < noBuild.radius)
          return true;
        // Negative value means the whole zone.
        if (noBuild.dungeon < 0f && point.y > 3000 && i == zone.x && j == zone.y)
          return true;
        if (point.y > 3000 && Utils.DistanceXZ(new(noBuild.X, 0, noBuild.Z), point) < noBuild.dungeon)
          return true;
      }
    }
    return false;
  }
  public static bool IsInsideNoBuildBiome(Vector3 point)
  {
    var biome = WorldGenerator.instance.GetBiome(point);
    return BiomeManager.TryGetData(biome, out var biomeData) && biomeData.noBuild;
  }
  public static void Load(string yaml)
  {
    if (yaml == "") return;
    try
    {
      var data = Yaml.Deserialize<NoBuildData>(yaml, "");
      Log.Info($"Reloading no build data ({data.Count} entries).");
      NoBuild = data.ToDictionary(data => ZoneSystem.GetZone(new(data.X, 0, data.Z)));
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }
}

[HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
public class IsInsideNoBuildLocation
{

  static bool Postfix(bool result, Vector3 point)
  {
    return result || NoBuildManager.IsInsideNoBuildZone(point) || NoBuildManager.IsInsideNoBuildBiome(point);
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Load))]
public class ZoneSystemLoad
{
  static void Postfix()
  {
    if (Helper.IsClient()) return;
    NoBuildManager.UpdateData();
  }
}