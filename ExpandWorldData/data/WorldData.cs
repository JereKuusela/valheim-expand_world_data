using System.ComponentModel;
using Service;
using YamlDotNet.Serialization;
namespace ExpandWorldData;

public class WorldData
{
  public string biome = "";
  [DefaultValue(10000f)]
  public float maxAltitude = 10000f;
  [DefaultValue(-1000f)]
  public float minAltitude = -1000f;
  [DefaultValue(1f)]
  public float maxDistance = 1f;
  [DefaultValue(0f)]
  public float minDistance = 0f;
  [DefaultValue(1f)]
  public float maxSector = 1f;
  [DefaultValue(0f)]
  public float minSector = 0f;
  [DefaultValue(0f)]
  public float centerX = 0f;
  [DefaultValue(0f)]
  public float centerY = 0f;
  [DefaultValue(1f)]
  public float amount = 1f;
  [DefaultValue(1f)]
  public float stretch = 1f;
  [DefaultValue("")]
  public string seed = "";
  [DefaultValue(true)]
  public bool wiggleDistance = true;
  [DefaultValue(true)]
  public bool wiggleSector = true;
  [DefaultValue("")]
  public string boiling = "";
}


public class WorldEntry
{
  private static float ConvertDist(float percent) => percent * WorldInfo.Radius;
  public WorldEntry(WorldData data)
  {
    biome = DataManager.ToBiomes(data.biome);
    biomeSeed = BiomeManager.GetTerrain(biome);
    if (Parse.TryInt(data.seed, out var s))
      seed = s;
    else if (BiomeManager.TryGetBiome(data.seed, out var biome))
      biomeSeed = biome;

    if (data.boiling == "" && biome == Heightmap.Biome.AshLands)
      boiling = 1f;
    if (data.boiling == "true")
      boiling = 1f;
    if (Parse.TryFloat(data.boiling, out var b))
      boiling = b;

    maxAltitude = data.maxAltitude;
    minAltitude = data.minAltitude;
    maxDistance = ConvertDist(data.maxDistance);
    minDistance = ConvertDist(data.minDistance);
    maxSector = data.maxSector;
    minSector = data.minSector;
    centerX = ConvertDist(data.centerX);
    centerY = ConvertDist(data.centerY);
    amount = data.amount;
    stretch = data.stretch;
    wiggleDistance = data.wiggleDistance;
    wiggleSector = data.wiggleSector;

    if (minSector < 0f) minSector += 1f;
    if (maxSector > 1f) maxSector -= 1f;
  }
  public Heightmap.Biome biome = Heightmap.Biome.None;
  public float maxAltitude = 10000f;
  public float minAltitude = -1000f;
  public float maxDistance = 1f;
  public float minDistance = 0f;
  public float maxSector = 1f;
  public float minSector = 0f;
  public float centerX = 0f;
  public float centerY = 0f;
  public float amount = 1f;
  public float stretch = 1f;
  public Heightmap.Biome biomeSeed = Heightmap.Biome.None;
  public int? seed = null;
  public bool wiggleDistance = true;
  public bool wiggleSector = true;
  public float boiling = 0f;
}
