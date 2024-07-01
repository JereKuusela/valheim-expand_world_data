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
  public static List<WorldData> DefaultData = [
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
        biome = "mountain",
        minAltitude = 50f,
      },
      new() {
        biome = "deepnorth",
        centerY = -0.4f,
        minDistance = 1.2f,
        maxDistance = 1.6f
      },
      new() {
        biome = "swamp",
        wiggleDistanceWidth = 0f,
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
  public static List<WorldEntry> DefaultEntries = DefaultData.Select(s => new WorldEntry(s)).ToList();

  public static List<WorldData> Data = DefaultData;

  public static WorldData ToData(WorldData biome) => biome;

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataWorld) return;
    if (File.Exists(FilePath)) return;
    var yaml = Yaml.Serializer().Serialize(DefaultData);
    File.WriteAllText(FilePath, yaml);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = Configuration.DataWorld ? DataManager.Read(Pattern) : "";
    Configuration.valueWorldData.Value = yaml;
    Set(yaml);
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  private static void Set(string yaml)
  {
    if (yaml == "" || !Configuration.DataWorld) return;
    try
    {
      Data = Yaml.Deserialize<WorldData>(yaml, FileName);
      if (Data.Count == 0)
      {
        Log.Warning($"Failed to load any world data.");
        Log.Info($"Reloading default world data ({Data.Count} entries).");
        Data = DefaultData;
      }
      else
        Log.Info($"Reloading world data ({Data.Count} entries).");
      BiomeCalculator.Data = Data.Select(s => new WorldEntry(s)).ToList(); ;
      BiomeCalculator.CheckAngles = Data.Any(x => x.minSector != 0f || x.maxSector != 1f);
      EWD.Instance.InvokeRegenerate();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }
  public static void Reload()
  {
    Log.Info($"Reloading world data ({Data.Count} entries).");
    BiomeCalculator.Data = Data.Select(s => new WorldEntry(s)).ToList(); ;
    BiomeCalculator.CheckAngles = Data.Any(x => x.minSector != 0f || x.maxSector != 1f);
    EWD.Instance.InvokeRegenerate();
  }
  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, FromFile);
  }
}