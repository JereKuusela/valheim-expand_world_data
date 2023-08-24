
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExpandWorldData;

public class WorldInfo
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
    if (WorldGenerator.instance == null) return;
    EWD.Log.LogInfo("Regenerating the world.");
    WorldGenerator.instance.Pregenerate();
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
    if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
      Minimap.instance?.GenerateWorldMap();
  }
}


[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate)), HarmonyPriority(Priority.HigherThanNormal)]
public class Pregenerate
{
  static void Prefix(WorldGenerator __instance)
  {
    // River points must at least be cleaned.
    // But better clean up everything.
    __instance.m_riverCacheLock.EnterWriteLock();
    __instance.m_riverPoints = [];
    __instance.m_rivers = [];
    __instance.m_streams = [];
    __instance.m_lakes = [];
    __instance.m_cachedRiverGrid = new(-999999, -999999);
    __instance.m_cachedRiverPoints = [];
    __instance.m_riverCacheLock.ExitWriteLock();
  }
}
