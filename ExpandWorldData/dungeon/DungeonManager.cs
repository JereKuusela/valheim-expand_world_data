using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Service;

namespace ExpandWorldData.Dungeon;

// Dungeons don't have configuration and appear as part of locations.
// So compared to other data,  the default dungeon generators are never removed.
[HarmonyPatch]
public partial class DungeonManager
{
  public static string FileName = "expand_dungeons.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_dungeons*.yaml";
  private static readonly List<DungeonYaml> ExtraDungeonYamls = [];

  private static Dictionary<string, DungeonGenerator> DefaultGenerators = [];
  private class Config : YamlDataManager
  {
    protected override string FilePath => DungeonManager.FilePath;
    protected override string Pattern => DungeonManager.Pattern;
    protected override bool DataEnabled => Configuration.DataDungeons;
    protected override SetResult Set(Dictionary<string, string> files) => DungeonManager.Set(files);
    protected override void WriteDefaultConfig() => ToFile();
  }
  private static readonly Config Instance = new();

  public static void AddDungeon(DungeonYaml yaml)
  {
    ExtraDungeonYamls.Add(yaml);
  }

  public static void Initialize()
  {
    EnvironmentBox.Cache.Clear();
    DungeonObjects.Generators = [];
    DefaultGenerators.Clear();
    if (Helper.IsServer())
    {
      DefaultGenerators = ZNetScene.instance.m_namedPrefabs.Values.Where(prefab => prefab.GetComponent<DungeonGenerator>())
        .Select(prefab => prefab.GetComponent<DungeonGenerator>()).ToDictionary(kvp => kvp.name, kvp => kvp);
    }
    Migrations.RunDungeons(DefaultGenerators);
    ReadConfigs();
  }

  private static void ToFile()
  {
    var data = DefaultGenerators.Values.Select(To).ToList();
    if (ExtraDungeonYamls.Count > 0)
      data.AddRange(ExtraDungeonYamls);
    Save(data, false);
  }

  public static void ReadConfigs() => Instance.ReadConfigs();

  private static SetResult Set(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      DungeonObjects.Generators = [];
      Log.Info("Reloading default dungeon data.");
      return SetResult.Applied;
    }
    Dictionary<string, FakeDungeonGenerator> data = [];
    foreach (var file in files)
    {
      if (!Yaml.TryDeserialize<DungeonYaml>(file.Value, file.Key, out var parsed)) return SetResult.Failed;
      foreach (var entry in parsed)
      {
        var generator = From(entry, file.Key);
        data.Add(generator.name, generator);
      }
    }
    if (data.Count == 0)
    {
      Log.Warning("Failed to load any dungeon data.");
      return SetResult.Failed;
    }
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(data))
      return SetResult.Rewritten;
    Log.Info($"Reloading dungeon data ({data.Count} entries).");
    DungeonObjects.Generators = data;
    return SetResult.Applied;
  }

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  private static bool AddMissingEntries(Dictionary<string, FakeDungeonGenerator> entries)
  {
    var missingKeys = DefaultGenerators.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    foreach (var kvp in entries)
      missingKeys.Remove(kvp.Key);
    if (missingKeys.Count == 0) return false;
    Log.Warning($"Adding {missingKeys.Count} missing dungeon generators to the expand_dungeons.yaml file.");
    var missingData = missingKeys.Values.Select(To).ToList();
    Save(missingData, true);
    return true;
  }
  private static void Save(List<DungeonYaml> data, bool log)
  {
    YamlFiles.SavePerMod("dungeons", data, item => item.name, log);
  }

  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataDungeons, ReadConfigs);
  }
}
