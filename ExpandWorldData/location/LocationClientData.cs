using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using Service;

namespace ExpandWorldData;

// Subset of the location data that is needed by clients (and by whoever owns the spawners).
public class LocationClientYaml
{
  public string name = "";
  [DefaultValue("")]
  public string discoverLabel = "";
  [DefaultValue("")]
  public string noBuild = "";
  [DefaultValue(0f)]
  public float exteriorRadius = 0f;
  [DefaultValue(0f)]
  public float interiorRadius = 0f;
  [DefaultValue("")]
  public string interiorEnvironment = "";
  [DefaultValue(-1)]
  public int enemyMinLevel = -1;
  [DefaultValue(-1)]
  public int enemyMaxLevel = -1;
  [DefaultValue(-1f)]
  public float enemyLevelUpChance = -1f;
  [DefaultValue("")]
  public string enemyLevelExcludeGroups = "";
  [DefaultValue("")]
  public string blockSpawnGroups = "";
}

///<summary>Synced table (by location name) of Location component values. Applied when the location prefab is instantiated.</summary>
public static class LocationClientData
{
  private static bool Initialized;
  private static bool Pending;
  private static Dictionary<int, LocationClientYaml> Table = [];
  // Location.Awake runs inside the proxy spawn, before the location is parented to the proxy.
  private static LocationClientYaml? Current;

  public static bool HasData => Table.Count > 0;

  public static void Initialize() => Initialized = true;

  public static void ApplyPending()
  {
    if (!Pending) return;
    Pending = false;
    Apply(Configuration.valueLocationClientData.Value);
  }

  public static void CleanUp()
  {
    Initialized = false;
    Pending = false;
    Table.Clear();
    Current = null;
  }

  public static void UpdateData()
  {
    List<LocationClientYaml> list = [];
    HashSet<string> names = [];
    foreach (var kvp in LocationExtra.ExtraInfo)
    {
      var name = kvp.Key.m_prefab.Name;
      // Blueprints have no Location component. The first entry wins like in ZoneSystem.GetLocation.
      if (BlueprintManager.Has(name) || !names.Add(name)) continue;
      list.Add(ToClient(name, kvp.Value.Data));
    }
    Configuration.valueLocationClientData.Value = list.Count == 0 ? "" : Yaml.Serializer().Serialize(list);
    SetTable(list);
    Refresh.Patches();
  }

  private static LocationClientYaml ToClient(string name, LocationYaml data) => new()
  {
    name = name,
    discoverLabel = data.discoverLabel,
    noBuild = data.noBuild == "false" ? "" : data.noBuild,
    exteriorRadius = data.exteriorRadius,
    interiorRadius = data.interiorRadius,
    interiorEnvironment = data.interiorEnvironment,
    enemyMinLevel = data.enemyMinLevel,
    enemyMaxLevel = data.enemyMaxLevel,
    enemyLevelUpChance = data.enemyLevelUpChance,
    enemyLevelExcludeGroups = data.enemyLevelExcludeGroups,
    blockSpawnGroups = data.blockSpawnGroups,
  };

  private static void SetTable(List<LocationClientYaml> list)
  {
    Table = [];
    foreach (var item in list)
    {
      var hash = item.name.GetStableHashCode();
      if (!Table.ContainsKey(hash)) Table[hash] = item;
    }
    foreach (var location in Location.s_allLocations)
    {
      if (location && GetLocationData(location.GetComponentInParent<LocationProxy>()) != null)
        location.m_noBuild = false;
    }
  }

  public static void Apply(string yaml)
  {
    if (Helper.IsClient() && !Initialized)
    {
      Pending = true;
      return;
    }
    // Server builds the table directly from its own data.
    if (Helper.IsServer()) return;
    try
    {
      SetTable(yaml == "" ? [] : Yaml.Deserialize<LocationClientYaml>(yaml, "Location client data"));
      Log.Info($"Reloading location client data ({Table.Count} entries).");
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    finally { Refresh.Patches(); }
  }

  public static void Patch(Harmony harmony)
  {
    var shouldPatch = HasData;
    Patches.Apply(harmony, shouldPatch, typeof(LocationProxy), nameof(LocationProxy.SpawnLocation), typeof(LocationClientData), nameof(SetContext), HarmonyPatchType.Prefix);
    Patches.Apply(harmony, shouldPatch, typeof(LocationProxy), nameof(LocationProxy.SpawnLocation), typeof(LocationClientData), nameof(ClearContext), HarmonyPatchType.Finalizer);
    Patches.Apply(harmony, shouldPatch, typeof(Location), nameof(Location.Awake), typeof(LocationClientData), nameof(ApplyToLocation), HarmonyPatchType.Prefix);
  }

  internal static void SetContext(LocationProxy __instance)
  {
    Current = GetLocationData(__instance);
  }

  private static LocationClientYaml? GetLocationData(LocationProxy? proxy)
  {
    if (!proxy) return null;
    var zdo = proxy.m_nview?.GetZDO();
    if (zdo == null) return null;
    // Reference hash keeps the full name (clones), while vanilla hash only has the base name.
    var hash = zdo.GetInt(FixGhostInit.ReferenceHash, 0);
    if (hash == 0) hash = zdo.GetInt(ZDOVars.s_location, 0);
    return Table.TryGetValue(hash, out var data) ? data : null;
  }

  internal static void ClearContext() => Current = null;

  internal static void ApplyToLocation(Location __instance)
  {
    var data = Current;
    if (data == null) return;
    __instance.m_discoverLabel = data.discoverLabel;
    if (data.exteriorRadius > 0f) __instance.m_exteriorRadius = data.exteriorRadius;
    if (data.interiorRadius > 0f) __instance.m_interiorRadius = data.interiorRadius;
    __instance.m_interiorEnvironment = data.interiorEnvironment;
    __instance.m_noBuild = false;
    __instance.m_enemyMinLevelOverride = data.enemyMinLevel;
    __instance.m_enemyMaxLevelOverride = data.enemyMaxLevel;
    __instance.m_enemyLevelUpOverride = data.enemyLevelUpChance;
    __instance.m_excludeEnemyLevelOverrideGroups = ParseGroups(data.enemyLevelExcludeGroups);
    __instance.m_blockSpawnGroups = ParseGroups(data.blockSpawnGroups);
  }

  private static List<int> ParseGroups(string groups) => [.. Parse.Split(groups).Select(group => Parse.Int(group))];
}
