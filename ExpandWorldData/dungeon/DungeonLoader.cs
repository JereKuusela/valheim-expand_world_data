using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;

namespace ExpandWorldData.Dungeon;

// Dungeons don't have configuration and appear as part of locations.
// So compared to other data,  the default dungeon generators are never removed.
// So handling missing entries isn't very important but can be added later.
[HarmonyPatch]
public partial class Loader
{
  public static string FileName = "expand_dungeons.yaml";
  public static string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  public static string Pattern = "expand_dungeons*.yaml";

  private static Dictionary<string, DungeonGenerator> DefaultGenerators = new();

  public static void Initialize()
  {
    EnvironmentBox.Cache.Clear();
    DefaultGenerators.Clear();
    if (Helper.IsServer())
    {
      // Dungeons don't have configuration so the data must be pulled from locations.
      DefaultGenerators = ZoneSystem.instance.m_locations
        .Select(loc => loc.m_prefab ? loc.m_prefab.GetComponentInChildren<DungeonGenerator>() : null!)
        .Where(dg => dg != null)
        .Distinct(new DgComparer()).ToDictionary(kvp => kvp.name, kvp => kvp);
    }
    Load();
  }

  private static void ToFile()
  {
    var yaml = DataManager.Serializer().Serialize(DefaultGenerators.Select(kvp => To(kvp.Value)).ToList());
    File.WriteAllText(FilePath, yaml);
  }

  private static Dictionary<string, FakeDungeonGenerator> FromFile()
  {
    try
    {
      var data = DataManager.Deserialize<DungeonData>(DataManager.Read(Pattern), FileName);
      return data.ToDictionary(data => data.name, From);
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
    return new();
  }

  public static void Load()
  {
    DungeonObjects.Generators.Clear();
    if (Helper.IsClient()) return;
    if (!Configuration.DataRooms)
    {
      EWD.Log.LogInfo($"Reloading default dungeon entries).");
      return;
    }
    if (!File.Exists(FilePath))
    {
      ToFile();
      return; // Watcher triggers reload.
    }

    var data = FromFile();
    if (data.Count == 0)
    {
      EWD.Log.LogWarning($"Failed to load any dungeon data.");
      EWD.Log.LogInfo($"Reloading default dungeon data.");
      return;
    }
    if (Configuration.DataMigration && AddMissingEntries(data))
    {
      // Watcher triggers reload.
      return;
    }
    EWD.Log.LogInfo($"Reloading dungeon data ({data.Count} entries).");
    DungeonObjects.Generators = data;
  }

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  private static bool AddMissingEntries(Dictionary<string, FakeDungeonGenerator> entries)
  {
    var missingKeys = DefaultGenerators.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    foreach (var kvp in entries)
      missingKeys.Remove(kvp.Key);
    if (missingKeys.Count == 0) return false;
    EWD.Log.LogWarning($"Adding {missingKeys.Count} missing dungeon generators to the expand_dungeons.yaml file.");
    foreach (var kvp in missingKeys)
      EWD.Log.LogWarning(kvp.Key);
    var yaml = File.ReadAllText(FilePath);
    var data = DataManager.Serializer().Serialize(missingKeys.Select(kvp => To(kvp.Value)).ToList());
    // Directly appending is risky but necessary to keep comments, etc.
    yaml += "\n" + data;
    File.WriteAllText(FilePath, yaml);
    return true;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
  }
}

class DgComparer : IEqualityComparer<DungeonGenerator>
{
  public bool Equals(DungeonGenerator dg1, DungeonGenerator dg2)
  {
    return dg1.name == dg2.name;
  }

  public int GetHashCode(DungeonGenerator dg)
  {
    return dg.name.GetHashCode();
  }
}