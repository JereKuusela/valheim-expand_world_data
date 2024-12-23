using System.ComponentModel;
using Service;
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

  [DefaultValue(null)]
  public float? wiggleDistanceLength;
  [DefaultValue(null)]
  public float? wiggleDistanceWidth;
  [DefaultValue(null)]
  public float? wiggleSectorLength;
  [DefaultValue(null)]
  public float? wiggleSectorWidth;
  [DefaultValue("")]
  public string boiling = "";
}


public class WorldEntry
{
  public static float ConvertDist(float percent) => percent * WorldInfo.Radius;
  public WorldEntry(WorldData data)
  {
    biome = DataManager.ToBiomes(data.biome);
    biomeSeed = BiomeManager.GetTerrain(biome);
    if (Parse.TryInt(data.seed, out var s))
      seed = s;
    else if (BiomeManager.TryGetBiome(data.seed, out var biome))
      biomeSeed = biome;

    if (data.boiling == "" && BiomeManager.TryGetData(biome, out var biomeData) && biomeData.lava)
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
    wiggleDistanceLength = Configuration.WiggleFrequency;
    wiggleDistanceWidth = Configuration.WiggleWidth;
    wiggleSectorLength = Configuration.DistanceWiggleLength;
    wiggleSectorWidth = Configuration.DistanceWiggleWidth;
    if (!data.wiggleDistance)
      wiggleDistanceWidth = 0f;
    if (!data.wiggleSector)
      wiggleSectorWidth = 0f;
    if (data.wiggleDistanceLength.HasValue)
      wiggleDistanceLength = data.wiggleDistanceLength.Value;
    if (data.wiggleDistanceWidth.HasValue)
      wiggleDistanceWidth = data.wiggleDistanceWidth.Value;
    if (data.wiggleSectorLength.HasValue)
      wiggleSectorLength = data.wiggleSectorLength.Value;
    if (data.wiggleSectorWidth.HasValue)
      wiggleSectorWidth = data.wiggleSectorWidth.Value;

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
  public float wiggleDistanceLength = 20f;
  public float wiggleDistanceWidth = 100f;
  public float wiggleSectorLength = 500f;
  public float wiggleSectorWidth = 0.01f;
  public float boiling = 0f;
}
