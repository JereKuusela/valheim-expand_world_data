using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData.Spawn;
using Service;

namespace ExpandWorldData.Migration;

internal static class SpawnMigration
{
  public static readonly Dictionary<string, string> Renames = new()
  {
    ["requiredGlobalKey"] = "requiredGlobalKeys",
    ["forbiddenGlobalKey"] = "forbiddenGlobalKeys",
    ["requiredLocation"] = "requiredLocations",
    ["forbiddenLocation"] = "forbiddenLocations",
    ["requiredObject"] = "requiredObjects",
    ["forbiddenObject"] = "forbiddenObjects",
  };

  ///<summary>Backfills requiredPersistentEvent for yaml files saved before the field existed, by pulling values from vanilla data.</summary>
  public static void MigratePersistentEvents(IEnumerable<SpawnSystem.SpawnData> vanilla, Action reload)
  {
    var files = Migrations.ReadFiles<Spawn.Data>(SpawnManager.Pattern);
    var eligible = files.Keys.Where(file => !Yaml.HasField([file], "requiredPersistentEvent")).ToList();
    if (!BackfillPersistentEvents(vanilla, eligible)) return;
    Yaml.IgnoreWrites(SpawnManager.Pattern);
    reload();
  }

  private static bool BackfillPersistentEvents(IEnumerable<SpawnSystem.SpawnData> vanilla, List<string> files)
  {
    // Only prefabs whose vanilla entries all agree on one event can be keyed safely.
    var migrations = vanilla
      .Where(spawn => spawn.m_prefab)
      .GroupBy(spawn => spawn.m_prefab.name)
      .Where(group => group.Select(spawn => spawn.m_requiredPersistentEvent).Distinct().Count() == 1 && !string.IsNullOrEmpty(group.First().m_requiredPersistentEvent))
      .ToDictionary(group => group.Key, group => group.First().m_requiredPersistentEvent);
    if (migrations.Count == 0) return false;
    if (!Yaml.InsertMissingField(files, "prefab", "requiredPersistentEvent", migrations)) return false;
    Log.Warning($"Added requiredPersistentEvent to {migrations.Count} prefabs in {SpawnManager.Pattern} files.");
    return true;
  }
}