
using ExpandWorldData.World;
using HarmonyLib;

// Namespace kept as ExpandWorldData for LPA compatibility (looked up by name).
// Ideally the LAP author would use API instead, something to consider for future compatibility.
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
    // This check might have been removed at some point but it's good to prevent double regen.
    if (Radius == radius && TotalRadius == totalRadius && Stretch == stretch && BiomeStretch == biomeStretch) return;
    Radius = radius;
    TotalRadius = totalRadius;
    Stretch = stretch;
    BiomeStretch = biomeStretch;
    WorldManager.Reload();
  }
  public static void SetWaterLevel(float waterLevel)
  {
    WaterLevel = waterLevel;
  }
}
