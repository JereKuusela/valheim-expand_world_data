using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;
using Common;

namespace ExpandWorldData;

public class WorldManager
{
  public static string FileName = "expand_world.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_world*.yaml";

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => WorldManager.FilePath;
    protected override string Pattern => WorldManager.Pattern;
    protected override bool DataEnabled => Configuration.DataWorld;
    protected override string ConfigValue { get => Configuration.valueWorldData.Value; set => Configuration.valueWorldData.Value = value; }
    protected override bool Set(Dictionary<string, string> files) => WorldManager.Set(files);
    protected override void WriteDefaultConfig() => File.WriteAllText(FilePath, Yaml.Serializer().Serialize(DefaultData));
  }
  private static readonly Sync Instance = new();

  public static List<WorldYaml> DefaultData = [
      new() {
        biome = "ashlands",
        centerY = 0.4f,
        minDistance = 1.2f,
        maxDistance = 1.6f,
        boiling = "true"
      },
      new() {
        biome = "ocean",
        maxAltitude = -26f
      },
      new() {
        biome = "deepnorth",
        centerY = -0.4f,
        minDistance = 1.2f,
        maxDistance = 1.6f
      },
      new() {
        biome = "mountain",
        minAltitude = 50f,
      },
      new() {
        biome = "swamp",
        wiggleDistance = false,
        minDistance = 0.2f,
        maxDistance = 0.6f,
        minAltitude = -20f,
        maxAltitude = 20f,
        amount = 0.4f,
      },
      new() {
        biome = "mistlands",
        minDistance = 0.6f,
        amount = 0.6f,
      },
      new() {
        biome = "plains",
        minDistance = 0.3f,
        maxDistance = 0.8f,
        amount = 0.6f,
      },
      new() {
        biome = "blackforest",
        minDistance = 0.06f,
        maxDistance = 0.6f,
        amount = 0.6f,
      },
      new() {
        biome = "blackforest",
        minDistance = 0.5f,
      },
      new() {
        biome = "meadows",
      },
    ];
  // Previous default had deepnorth and mountain swapped.
  private static readonly List<WorldYaml> OldData = SwapDeepNorthAndMountain(DefaultData);
  private static List<WorldYaml> SwapDeepNorthAndMountain(List<WorldYaml> source)
  {
    var list = new List<WorldYaml>(source);
    var a = list.FindIndex(x => x.biome == "deepnorth");
    var b = list.FindIndex(x => x.biome == "mountain");
    (list[a], list[b]) = (list[b], list[a]);
    return list;
  }
  public static List<WorldEntry> DefaultEntries = [.. DefaultData.Select(s => new WorldEntry(s, "default world"))];
  public static void AddWorld(WorldYaml data, int index)
  {
    DefaultData.Insert(index, data);
    DefaultEntries.Insert(index, new WorldEntry(data, ""));
  }
  public static List<WorldYaml> Data = DefaultData;
  public static bool HasData { get; private set; }

  public static WorldYaml ToData(WorldYaml biome) => biome;

  public static void CreateConfigs() => Instance.CreateConfigs();
  public static void ReadConfigs() => Instance.ReadConfigs();
  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);
  private static bool Set(Dictionary<string, string> files)
  {
    try
    {
      if (files.Count == 0)
      {
        HasData = false;
        Refresh.Request(Regen.World);
        return true;
      }
      List<WorldYaml> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<WorldYaml>(file.Value, file.Key, out var parsed))
          return false;
        data.AddRange(parsed);
      }
      if (Configuration.DataMigration && Helper.IsServer() && data.Count == OldData.Count && Yaml.Serializer().Serialize(data) == Yaml.Serializer().Serialize(OldData))
      {
        Log.Info("Detected old default world data with swapped deepnorth and mountain. Using fixed default.");
        data = DefaultData;
        if (File.Exists(FilePath))
          File.Delete(FilePath);
        CreateConfigs();
        // Watcher triggers erload.
      }
      if (data.Count == 0)
      {
        Log.Warning($"Failed to load any world data.");
        Log.Info($"Reloading default world data ({data.Count} entries).");
        data = DefaultData;
      }
      else
        Log.Info($"Reloading world data ({data.Count} entries).");
      var entries = data.Select(s => new WorldEntry(s, "world")).ToList();
      BiomeCalculator.SetData(entries);
      BiomeCalculator.CheckAngles = data.Any(x => x.minSector != 0f || x.maxSector != 1f);
      Data = data;
      HasData = data.Count > 0;
      Refresh.Request(Regen.World);
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return false;
    }
  }
  public static void Reload()
  {
    Log.Info($"Reloading world data ({Data.Count} entries).");
    BiomeCalculator.SetData([.. Data.Select(s => new WorldEntry(s, "world"))]);
    BiomeCalculator.CheckAngles = Data.Any(x => x.minSector != 0f || x.maxSector != 1f);
    GetRandomPointByBiome.Warned.Clear();
  }
  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataWorld, ReadConfigs);
  }
}