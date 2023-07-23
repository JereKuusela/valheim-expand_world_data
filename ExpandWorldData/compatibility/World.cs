
using UnityEngine;

namespace ExpandWorldData;

public class World
{
  public static float WaterLevel = 30f;
  public static float Radius = 10000f;
  public static float TotalRadius = 10500f;
  public static float Stretch = 1f;
  public static float BiomeStretch = 1f;


  public static void Set(float radius, float totalRadius, float stretch, float biomeStretch)
  {
    Radius = radius;
    TotalRadius = totalRadius;
    Stretch = stretch;
    BiomeStretch = biomeStretch;
  }
  public static void SetWaterLevel(float waterLevel)
  {
    WaterLevel = waterLevel;
  }
  public static void AutomaticRegenerate()
  {
    WorldGenerator.instance?.Pregenerate();
    foreach (var heightmap in Object.FindObjectsOfType<Heightmap>())
    {
      heightmap.m_buildData = null;
      heightmap.Regenerate();
    }
    ClutterSystem.instance?.ClearAll();
    if (Configuration.RegenerateMap) RegenerateMap();
  }
  public static void RegenerateMap()
  {
    Minimap.instance?.GenerateWorldMap();
  }
}
