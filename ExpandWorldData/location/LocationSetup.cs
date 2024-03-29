using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;
namespace ExpandWorldData;

public class LocationSetup
{
  ///<summary>Vanilla has invalid data. Easier to clean up here so other code doesn't have to worry about it.</summary>
  public static List<ZoneSystem.ZoneLocation> CleanUpLocations(List<ZoneSystem.ZoneLocation> locations) => locations.Where(loc => loc.m_prefab).ToList();

  ///<summary>Vanilla has some unused locations. Might as well do setup for them too.</summary>
  public static void SetupExtraLocations(List<ZoneSystem.ZoneLocation> locations)
  {
    var loaded = locations.Select(loc => loc.m_prefab.name).ToHashSet();
    var array = Resources.FindObjectsOfTypeAll<GameObject>();
    foreach (var obj in array)
    {
      if (obj.name != "_Locations") continue;
      var data = obj.GetComponentsInChildren<Location>(true);
      var extraLocations = data.Where(l => !loaded.Contains(l.gameObject.name)).ToArray();
      foreach (var location in extraLocations)
        locations.Add(SetupLocation(location));
    }
    if (loaded.Count != locations.Count)
      Log.Info($"Enabled {locations.Count - loaded.Count} extra locations.");
    UpdateHashes();
  }
  public static void UpdateHashes()
  {
    var zs = ZoneSystem.instance;
    zs.m_locationsByHash = Helper.ToDict(zs.m_locations, loc => loc.m_hash, loc => loc);
    //ExpandWorldData.Log.Debug($"Loaded {zs.m_locationsByHash.Count} zone hashes.");
  }
  // Copy paste from vanilla code.
  private static ZoneSystem.ZoneLocation SetupLocation(Location location)
  {
    ZoneSystem.ZoneLocation zoneLocation = new()
    {
      m_enable = false,
      m_maxTerrainDelta = 10f,
      m_randomRotation = false,
      m_prefab = location.gameObject
    };
    zoneLocation.m_prefabName = zoneLocation.m_prefab.name;
    zoneLocation.m_hash = zoneLocation.m_prefabName.GetStableHashCode();
    zoneLocation.m_location = location;
    zoneLocation.m_interiorRadius = (location.m_hasInterior ? location.m_interiorRadius : 0f);
    zoneLocation.m_exteriorRadius = location.m_exteriorRadius;
    if (location.m_interiorTransform && location.m_generator)
    {
      zoneLocation.m_interiorPosition = location.m_interiorTransform.localPosition;
      zoneLocation.m_generatorPosition = location.m_generator.transform.localPosition;
    }
    ZoneSystem.PrepareNetViews(zoneLocation.m_prefab, zoneLocation.m_netViews);
    ZoneSystem.PrepareRandomSpawns(zoneLocation.m_prefab, zoneLocation.m_randomSpawns);
    return zoneLocation;
  }
}
