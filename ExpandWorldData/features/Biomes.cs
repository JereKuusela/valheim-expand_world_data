using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace ExpandWorldData;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.WorldAngle))]
public class WorldAngle
{
  static bool Prefix(float wx, float wy, ref float __result)
  {
    __result = Mathf.Sin(Mathf.Atan2(wx, wy) * Configuration.WiggleFrequency);
    return false;
  }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), new[] { typeof(Heightmap.Biome) })]
public class GetBiomeColor
{
  static bool Prefix(Heightmap.Biome biome, ref Color32 __result)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return true;
    __result = data.color;
    return false;
  }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetPixelColor))]
public class GetMapColor
{
  static bool Prefix(Heightmap.Biome biome, ref Color __result)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return true;
    __result = data.mapColor;
    return false;
  }
}

[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiome))]
public class GetBiomeHM
{
  public static bool Nature = false;
  static Heightmap.Biome Postfix(Heightmap.Biome biome)
  {
    if (Nature) return BiomeManager.GetNature(biome);
    return biome;
  }
}


[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.FindBiome))]
public class HeightmapFindBiome
{
  public static bool Nature = false;
  static Heightmap.Biome Postfix(Heightmap.Biome biome)
  {
    if (Nature) return BiomeManager.GetNature(biome);
    return biome;
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Initialize))]
public class ResetBiomeOffsets
{
  static void Prefix()
  {
    GetBiomeWG.Offsets.Clear();
  }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GetMaskColor))]
public class GetMaskColor
{
  static void Prefix(ref Heightmap.Biome biome)
  {
    biome = BiomeManager.GetTerrain(biome);
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate))]
public class SetBiomeOffsets
{
  [HarmonyPriority(Priority.VeryHigh)]
  static void Prefix(WorldGenerator __instance)
  {
    if (GetBiomeWG.Offsets.Count > 0) return;
    GetBiomeWG.Offsets[Heightmap.Biome.Swamp] = __instance.m_offset0;
    GetBiomeWG.Offsets[Heightmap.Biome.Plains] = __instance.m_offset1;
    GetBiomeWG.Offsets[Heightmap.Biome.BlackForest] = __instance.m_offset2;
    // Not used in the base game code but might as well reuse the value.
    GetBiomeWG.Offsets[Heightmap.Biome.Meadows] = __instance.m_offset3;
    GetBiomeWG.Offsets[Heightmap.Biome.Mistlands] = __instance.m_offset4;
    GetBiomeWG.Offsets[Heightmap.Biome.AshLands] = Random.Range(-10000, 10000);
    GetBiomeWG.Offsets[Heightmap.Biome.DeepNorth] = Random.Range(-10000, 10000);
    GetBiomeWG.Offsets[Heightmap.Biome.Mountain] = Random.Range(-10000, 10000);
    GetBiomeWG.Offsets[Heightmap.Biome.Ocean] = Random.Range(-10000, 10000);
  }
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new[] { typeof(float), typeof(float) })]
public class GetBiomeWG
{
  public static List<WorldData> GetData() => Data ?? WorldManager.GetDefault(WorldGenerator.instance);
  public static List<WorldData>? Data = null;
  public static bool CheckAngles = false;
  public static Dictionary<Heightmap.Biome, float> Offsets = [];

  private static float GetOffset(WorldGenerator obj, Heightmap.Biome biome)
  {
    if (Offsets.TryGetValue(biome, out var value)) return value;
    return obj.m_offset0;
  }
  private static float ConvertDist(float percent) => percent * WorldInfo.Radius;

  // Remember to update the legacy version too.
  private static Heightmap.Biome Get(WorldGenerator obj, float wx, float wy)
  {
    Data ??= WorldManager.GetDefault(obj);
    var sx = wx * WorldInfo.Stretch;
    var sy = wy * WorldInfo.Stretch;
    var magnitude = new Vector2(sx, sy).magnitude;
    if (magnitude > WorldInfo.TotalRadius)
      return Heightmap.Biome.Ocean;
    var altitude = Helper.BaseHeightToAltitude(obj.GetBaseHeight(wx, wy, false));
    var num = obj.WorldAngle(wx, wy) * Configuration.WiggleWidth;
    var baseAngle = 0f;
    var wiggledAngle = 0f;
    if (CheckAngles)
    {
      baseAngle = (Mathf.Atan2(wx, wy) + Mathf.PI) / 2f / Mathf.PI;
      wiggledAngle = baseAngle + Configuration.DistanceWiggleWidth * Mathf.Sin(magnitude / Configuration.DistanceWiggleLength);
      if (wiggledAngle < 0f) wiggledAngle += 1f;
      if (wiggledAngle >= 1f) wiggledAngle -= 1f;
    }
    var radius = WorldInfo.Radius;
    var bx = wx / WorldInfo.BiomeStretch;
    var by = wy / WorldInfo.BiomeStretch;

    foreach (var item in Data)
    {
      if (item.minAltitude >= altitude || item.maxAltitude <= altitude) continue;
      var mag = magnitude;
      var min = ConvertDist(item.minDistance);
      if (min > 0)
        min += item.wiggleDistance ? num : 0f;
      else if (min == 0f)
        min = -0.1f; // To handle the center (0,0) correctly.
      var max = ConvertDist(item.maxDistance);
      if (item.centerX != 0f || item.centerY != 0f)
      {
        var centerX = ConvertDist(item.centerX);
        var centerY = ConvertDist(item.centerY);
        mag = new Vector2(sx - centerX, sy - centerY).magnitude;
      }
      if (item.curveX != 0f || item.curveY != 0f)
      {
        var curveX = ConvertDist(item.curveX);
        var curveY = ConvertDist(item.curveY);
        mag = new Vector2(sx + curveX, sy + curveY).magnitude;
        min += new Vector2(curveX, curveY).magnitude;
      }
      var distOk = mag > min && (max >= radius || mag < max);
      if (!distOk) continue;
      if (CheckAngles)
      {
        min = item.minSector;
        max = item.maxSector;
        if (min != 0f || max != 1f)
        {
          var angle = item.wiggleSector ? wiggledAngle : baseAngle;
          var angleOk = min > max ? (angle >= min || angle < max) : angle >= min && angle < max;
          if (!angleOk) continue;
        }
      }
      var seed = item._seed ?? GetOffset(obj, item._biomeSeed);
      if (item.amount < 1f && Mathf.PerlinNoise((seed + bx / item.stretch) * 0.001f, (seed + by / item.stretch) * 0.001f) <= 1 - item.amount) continue;
      return item._biome;
    }
    return Heightmap.Biome.Ocean;
  }

  // Bit annoying to maintain two versions of the same code.
  // But biome generation is performance critical so trying to keep it simple.
  private static Heightmap.Biome GetLegacy(WorldGenerator obj, float wx, float wy)
  {
    Data ??= WorldManager.GetDefault(obj);
    var sx = wx * WorldInfo.Stretch;
    var sy = wy * WorldInfo.Stretch;
    var magnitude = new Vector2(sx, sy).magnitude;
    if (magnitude > WorldInfo.TotalRadius)
      return Heightmap.Biome.Ocean;
    var altitude = Helper.BaseHeightToAltitude(obj.GetBaseHeight(wx, wy, false));
    var num = obj.WorldAngle(wx, wy) * Configuration.WiggleWidth;
    var baseAngle = 0f;
    var wiggledAngle = 0f;
    if (CheckAngles)
    {
      baseAngle = (Mathf.Atan2(wx, wy) + Mathf.PI) / 2f / Mathf.PI;
      wiggledAngle = baseAngle + Configuration.DistanceWiggleWidth * Mathf.Sin(magnitude / Configuration.DistanceWiggleLength);
      if (wiggledAngle < 0f) wiggledAngle += 1f;
      if (wiggledAngle >= 1f) wiggledAngle -= 1f;
    }
    var radius = WorldInfo.Radius;
    var bx = wx / WorldInfo.BiomeStretch;
    var by = wy / WorldInfo.BiomeStretch;

    foreach (var item in Data)
    {
      if (item.minAltitude > altitude || item.maxAltitude < altitude) continue;
      var mag = magnitude;
      var min = ConvertDist(item.minDistance);
      if (min > 0)
        min += item.wiggleDistance ? num : 0f;
      else if (min == 0f)
        min = -0.1f; // To handle the center (0,0) correctly.
      var max = ConvertDist(item.maxDistance);
      if (item.centerX != 0f || item.centerY != 0f)
      {
        var centerX = ConvertDist(item.centerX);
        var centerY = ConvertDist(item.centerY);
        mag = new Vector2(sx - centerX, sy - centerY).magnitude;
      }
      if (item.curveX != 0f || item.curveY != 0f)
      {
        var curveX = ConvertDist(item.curveX);
        var curveY = ConvertDist(item.curveY);
        mag = new Vector2(sx + curveX, sy + curveY).magnitude;
        min += new Vector2(curveX, curveY).magnitude;
      }
      var distOk = mag > min && (max >= radius || mag <= max);
      if (!distOk) continue;
      if (CheckAngles)
      {
        min = item.minSector;
        max = item.maxSector;
        if (min != 0f || max != 1f)
        {
          var angle = item.wiggleSector ? wiggledAngle : baseAngle;
          var angleOk = min > max ? (angle >= min || angle < max) : angle >= min && angle < max;
          if (!angleOk) continue;
        }
      }
      var seed = item._seed ?? GetOffset(obj, item._biomeSeed);
      if (item.amount < 1f && Mathf.PerlinNoise((seed + bx / item.stretch) * 0.001f, (seed + by / item.stretch) * 0.001f) < 1 - item.amount) continue;
      return item._biome;
    }
    return Heightmap.Biome.Ocean;
  }
  static bool Prefix(WorldGenerator __instance, float wx, float wy, ref Heightmap.Biome __result)
  {
    if (__instance.m_world.m_menu) return true;
    if (!Configuration.DataWorld) return true;
    if (Configuration.LegacyGeneration)
      __result = GetLegacy(__instance, wx, wy);
    else
      __result = Get(__instance, wx, wy);
    return false;
  }
}
