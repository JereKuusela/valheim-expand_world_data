using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData;

public class WorldManager
{
  public static string FileName = "expand_world.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_world*.yaml";
  private static bool Initialized;
  private static bool Pending;

  public static void Load()
  {
    Initialized = true;
    if (!Pending) return;
    Pending = false;
    FromSetting(Configuration.valueWorldData.Value);
  }

  public static void CleanUp()
  {
    Initialized = false;
    Pending = false;
  }

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
  public static List<WorldEntry> DefaultEntries = [.. DefaultData.Select(s => new WorldEntry(s, "default world"))];
  public static void AddWorld(WorldYaml data, int index)
  {
    DefaultData.Insert(index, data);
    DefaultEntries.Insert(index, new WorldEntry(data, ""));
  }
  public static List<WorldYaml> Data = DefaultData;

  public static WorldYaml ToData(WorldYaml biome) => biome;

  public static void CreateConfigs()
  {
    if (Helper.IsClient() || !Configuration.DataWorld) return;
    if (File.Exists(FilePath)) return;
    var yaml = Yaml.Serializer().Serialize(DefaultData);
    File.WriteAllText(FilePath, yaml);
  }
  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (Configuration.DataWorld)
    {
      if (File.Exists(FilePath))
      {
        var files = DataManager.Read(Pattern);
        if (files == null || !Set(files)) return;
        Configuration.valueWorldData.Value = string.Join("\n", files.Values);
      }
      else
      {
        // Watcher will trigger reload.
        CreateConfigs();
      }
    }
    else
    {
      if (Set([]))
        Configuration.valueWorldData.Value = "";
    }
  }
  public static void FromSetting(string yaml)
  {
    if (!Helper.IsClient()) return;
    if (!Initialized)
    {
      Pending = true;
      return;
    }
    Set(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }
  private static bool Set(Dictionary<string, string> files)
  {
    try
    {
      if (files.Count == 0)
      {
        Features.Patcher.SetWorldEnabled(false);
        EWD.Instance.InvokeRegenerate();
        return true;
      }
      List<WorldYaml> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<WorldYaml>(file.Value, file.Key, out var parsed))
          return false;
        data.AddRange(parsed);
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
      Features.Patcher.SetWorldEnabled(data.Count > 0);
      EWD.Instance.InvokeRegenerate();
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
    EWD.Instance.InvokeRegenerate();
  }
  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, ReadConfigs);
  }
}