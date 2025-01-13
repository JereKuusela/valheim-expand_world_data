using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;

namespace ExpandWorldData.Dungeon;

// Dungeons don't have configuration and appear as part of locations.
// So compared to other data,  the default dungeon generators are never removed.
// So handling missing entries isn't very important but can be added later.
[HarmonyPatch]
public partial class Loader
{
  public static string FileName = "expand_dungeons.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_dungeons*.yaml";

  private static Dictionary<string, DungeonGenerator> DefaultGenerators = [];

  public static void Initialize()
  {
    EnvironmentBox.Cache.Clear();
    DefaultGenerators.Clear();
    if (Helper.IsServer())
    {
      DefaultGenerators = ZNetScene.instance.m_namedPrefabs.Values.Where(prefab => prefab.GetComponent<DungeonGenerator>())
        .Select(prefab => prefab.GetComponent<DungeonGenerator>()).ToDictionary(kvp => kvp.name, kvp => kvp);
    }
    Load();
  }

  private static void ToFile()
  {
    Save([.. DefaultGenerators.Values], false);
  }

  private static Dictionary<string, FakeDungeonGenerator> FromFile()
  {
    try
    {
      var data = DataManager.ReadData<DungeonData, FakeDungeonGenerator>(Pattern, From);
      return data.ToDictionary(data => data.name);
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    return [];
  }

  public static void Load()
  {
    DungeonObjects.Generators.Clear();
    if (Helper.IsClient()) return;
    if (!Configuration.DataRooms)
    {
      Log.Info($"Reloading default dungeon entries).");
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
      Log.Warning($"Failed to load any dungeon data.");
      Log.Info($"Reloading default dungeon data.");
      return;
    }
    if (Configuration.DataMigration && AddMissingEntries(data))
    {
      // Watcher triggers reload.
      return;
    }
    Log.Info($"Reloading dungeon data ({data.Count} entries).");
    DungeonObjects.Generators = data;
  }

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  private static bool AddMissingEntries(Dictionary<string, FakeDungeonGenerator> entries)
  {
    Dictionary<string, List<DungeonGenerator>> perFile = [];
    var missingKeys = DefaultGenerators.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    foreach (var kvp in entries)
      missingKeys.Remove(kvp.Key);
    if (missingKeys.Count == 0) return false;
    Log.Warning($"Adding {missingKeys.Count} missing dungeon generators to the expand_dungeons.yaml file.");
    Save([.. missingKeys.Values], true);
    return true;
  }
  private static void Save(List<DungeonGenerator> data, bool log)
  {
    Dictionary<string, List<DungeonGenerator>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.name);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.name}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_dungeons{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value.Select(To));
      File.WriteAllText(file, yaml);
    }
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, Load);
  }
}
