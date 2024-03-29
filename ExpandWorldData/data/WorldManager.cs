using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData;

public class WorldManager
{
  public static string FileName = "expand_world.yaml";
  public static string FilePath = Path.Combine(Yaml.Directory, FileName);
  public static string Pattern = "expand_world*.yaml";
  public static List<WorldData> GetDefault(WorldGenerator obj)
  {
    var version = obj.m_world.m_worldGenVersion;
    return [
      new() {
        biome = "ashlands",
        _biome = Heightmap.Biome.AshLands,
        _biomeSeed = Heightmap.Biome.AshLands,
        centerY = 0.4f,
        minDistance = 1.2f,
        maxDistance = 1.6f
      },
      new() {
        biome = "ocean",
        _biome = Heightmap.Biome.Ocean,
        _biomeSeed = Heightmap.Biome.Ocean,
        maxAltitude = -26f
      },
      new() {
        biome = "mountain",
        _biome = Heightmap.Biome.Mountain,
        _biomeSeed = Heightmap.Biome.Mountain,
        minAltitude = 50f,
      },
      new() {
        biome = "deepnorth",
        _biome = Heightmap.Biome.DeepNorth,
        _biomeSeed = Heightmap.Biome.DeepNorth,
        centerY = -0.4f,
        minDistance = 1.2f,
        maxDistance = 1.6f
      },
      new() {
        biome = "swamp",
        _biome = Heightmap.Biome.Swamp,
        _biomeSeed = Heightmap.Biome.Swamp,
        wiggleDistance = false,
        minDistance = 0.2f,
        maxDistance = version > 1 ? 0.6f :0.8f,
        minAltitude = -20f,
        maxAltitude = 20f,
        amount = 0.4f,
      },
      new() {
        biome = "mistlands",
        _biome = Heightmap.Biome.Mistlands,
        _biomeSeed = Heightmap.Biome.Mistlands,
        minDistance = 0.6f,
        amount = version > 1 ? 0.6f : 0.5f,
      },
      new() {
        biome = "plains",
        _biome = Heightmap.Biome.Plains,
        _biomeSeed = Heightmap.Biome.Plains,
        minDistance = 0.3f,
        maxDistance = 0.8f,
        amount = 0.6f,
      },
      new() {
        biome = "blackforest",
        _biome = Heightmap.Biome.BlackForest,
        _biomeSeed = Heightmap.Biome.BlackForest,
        minDistance = 0.06f,
        maxDistance = 0.6f,
        amount = 0.6f,
      },
      new() {
        biome = "blackforest",
        _biome = Heightmap.Biome.BlackForest,
        _biomeSeed = Heightmap.Biome.BlackForest,
        minDistance = 0.5f,
      },
      new() {
        biome = "meadows",
        _biome = Heightmap.Biome.Meadows,
        _biomeSeed = Heightmap.Biome.Meadows,
      },
    ];
  }

  public static WorldData FromData(WorldData data)
  {
    data._biome = DataManager.ToBiomes(data.biome);
    data._biomeSeed = BiomeManager.GetTerrain(data._biome);
    if (int.TryParse(data.seed, out var seed))
      data._seed = seed;
    else if (BiomeManager.TryGetBiome(data.seed, out var biome))
      data._biomeSeed = biome;
    if (data.minSector < 0f) data.minSector += 1f;
    if (data.maxSector > 1f) data.maxSector -= 1f;
    return data;
  }
  public static WorldData ToData(WorldData biome) => biome;

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataWorld) return;
    if (File.Exists(FilePath)) return;
    var yaml = Yaml.Serializer().Serialize(GetBiomeWG.GetData().Select(ToData).ToList());
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
      var data = Yaml.Deserialize<WorldData>(yaml, FileName)
        .Select(FromData).ToList();
      if (data.Count == 0)
      {
        Log.Warning($"Failed to load any world data.");
        return;
      }
      Log.Info($"Reloading world data ({data.Count} entries).");
      GetBiomeWG.Data = data;
      GetBiomeWG.CheckAngles = data.Any(x => x.minSector != 0f || x.maxSector != 1f);
      EWD.Instance.InvokeRegenerate();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }
  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, FromFile);
  }
}