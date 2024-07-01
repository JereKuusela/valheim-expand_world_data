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
    BiomeCalculator.Offsets.Clear();
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
    if (BiomeCalculator.Offsets.Count > 0) return;
    BiomeCalculator.Offsets[Heightmap.Biome.Swamp] = __instance.m_offset0;
    BiomeCalculator.Offsets[Heightmap.Biome.Plains] = __instance.m_offset1;
    BiomeCalculator.Offsets[Heightmap.Biome.BlackForest] = __instance.m_offset2;
    // Not used in the base game code but might as well reuse the value.
    BiomeCalculator.Offsets[Heightmap.Biome.Meadows] = __instance.m_offset3;
    BiomeCalculator.Offsets[Heightmap.Biome.Mistlands] = __instance.m_offset4;
    BiomeCalculator.Offsets[Heightmap.Biome.AshLands] = Random.Range(-10000, 10000);
    BiomeCalculator.Offsets[Heightmap.Biome.DeepNorth] = Random.Range(-10000, 10000);
    BiomeCalculator.Offsets[Heightmap.Biome.Mountain] = Random.Range(-10000, 10000);
    BiomeCalculator.Offsets[Heightmap.Biome.Ocean] = Random.Range(-10000, 10000);
  }
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(float), typeof(float), typeof(float), typeof(bool))]
public class GetBiomeWG
{
  static bool Prefix(WorldGenerator __instance, float wx, float wy, float oceanLevel, bool waterAlwaysOcean, ref Heightmap.Biome __result)
  {
    if (__instance.m_world.m_menu) return true;
    if (!Configuration.DataWorld) return true;
    if (waterAlwaysOcean && __instance.GetHeight(wx, wy) <= oceanLevel)
    {
      __result = Heightmap.Biome.Ocean;
      return false;
    }
    if (Configuration.LegacyGeneration)
      __result = BiomeCalculator.GetLegacy(__instance, wx, wy);
    else
      __result = BiomeCalculator.Get(__instance, wx, wy);
    return false;
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), typeof(float), typeof(float))]
public class GetAshlandsOceanGradient
{
  static bool Prefix(float x, float y, ref float __result)
  {
    var wg = WorldGenerator.instance;
    if (wg.m_world.m_menu) return true;
    if (!Configuration.DataWorld) return true;
    __result = BiomeCalculator.GetBoiling(wg, x, y);
    return false;
  }
}

public class BiomeCalculator
{
  public static List<WorldEntry> GetData() => Data ?? WorldManager.DefaultEntries;
  public static List<WorldEntry>? Data = null;
  public static bool CheckAngles = false;
  public static Dictionary<Heightmap.Biome, float> Offsets = [];

  private static float GetOffset(WorldGenerator obj, Heightmap.Biome biome)
  {
    if (Offsets.TryGetValue(biome, out var value)) return value;
    return obj.m_offset0;
  }

  // Remember to update the legacy version too.
  public static Heightmap.Biome Get(WorldGenerator obj, float wx, float wy)
  {
    var angle = Mathf.Atan2(wx, wy);
    return GetEntry(obj, wx, wy, angle)?.biome ?? Heightmap.Biome.Ocean;
  }
  // Bit annoying to maintain two versions of the same code.
  // But biome generation is performance critical so trying to keep it simple.
  public static Heightmap.Biome GetLegacy(WorldGenerator obj, float wx, float wy)
  {
    var worldAngle = Mathf.Atan2(wx, wy);
    var data = GetData();
    var sx = wx * WorldInfo.Stretch;
    var sy = wy * WorldInfo.Stretch;
    var magnitude = new Vector2(sx, sy).magnitude;
    if (magnitude > WorldInfo.TotalRadius)
      return Heightmap.Biome.Ocean;
    var altitude = Helper.BaseHeightToAltitude(obj.GetBaseHeight(wx, wy, false));
    var baseAngle = 0f;
    if (CheckAngles)
      baseAngle = (worldAngle + Mathf.PI) / 2f / Mathf.PI;
    var radius = WorldInfo.Radius;
    var bx = wx / WorldInfo.BiomeStretch;
    var by = wy / WorldInfo.BiomeStretch;

    foreach (var item in data)
    {
      if (item.minAltitude > altitude || item.maxAltitude < altitude) continue;
      var mag = magnitude;
      var min = item.minDistance;
      if (min > 0 && item.wiggleDistanceWidth > 0f)
        min += Mathf.Sin(worldAngle * item.wiggleDistanceLength);
      else if (min == 0f)
        min = -0.1f; // To handle the center (0,0) correctly.
      var max = item.maxDistance;
      if (item.centerX != 0f || item.centerY != 0f)
      {
        mag = new Vector2(sx - item.centerX, sy - item.centerY).magnitude;
      }
      var distOk = mag > min && (max >= radius || mag <= max);
      if (!distOk) continue;
      if (CheckAngles)
      {
        min = item.minSector;
        max = item.maxSector;
        if (min != 0f || max != 1f)
        {
          var angle = baseAngle;
          if (item.wiggleSectorWidth > 0f)
            angle += Mathf.Sin(magnitude / item.wiggleSectorLength) * item.wiggleSectorWidth;
          if (angle < 0f) angle += 1f;
          if (angle >= 1f) angle -= 1f;
          var angleOk = min > max ? (angle >= min || angle < max) : angle >= min && angle < max;
          if (!angleOk) continue;
        }
      }
      var seed = item.seed ?? GetOffset(obj, item.biomeSeed);
      if (item.amount < 1f && Mathf.PerlinNoise((seed + bx / item.stretch) * 0.001f, (seed + by / item.stretch) * 0.001f) < 1 - item.amount) continue;
      return item.biome;
    }
    return Heightmap.Biome.Ocean;
  }

  public static float GetBoiling(WorldGenerator obj, float wx, float wy)
  {
    var angle = Mathf.Atan2(wx, wy);
    var item = GetEntry(obj, wx, wy, angle);
    if (item == null || item.boiling <= 0f) return -1f;
    var sx = wx * WorldInfo.Stretch;
    var sy = wy * WorldInfo.Stretch;
    var dist = DUtils.Length(sx - item.centerX, sy - item.centerY);
    var min = item.minDistance;
    if (min > 0 && item.wiggleDistanceWidth > 0f)
      min += Mathf.Sin(angle * item.wiggleDistanceLength);
    else if (min == 0f)
      min = -0.1f; // To handle the center (0,0) correctly.
    return item.boiling * (dist - min) / 300f;
  }

  private static WorldEntry? GetEntry(WorldGenerator obj, float wx, float wy, float worldAngle)
  {
    var data = GetData();
    var sx = wx * WorldInfo.Stretch;
    var sy = wy * WorldInfo.Stretch;
    var magnitude = new Vector2(sx, sy).magnitude;
    if (magnitude > WorldInfo.TotalRadius)
      return null;
    var altitude = Helper.BaseHeightToAltitude(obj.GetBaseHeight(wx, wy, false));
    var baseAngle = 0f;
    if (CheckAngles)
      baseAngle = (worldAngle + Mathf.PI) / 2f / Mathf.PI;
    var radius = WorldInfo.Radius;
    var bx = wx / WorldInfo.BiomeStretch;
    var by = wy / WorldInfo.BiomeStretch;

    foreach (var item in data)
    {
      if (item.minAltitude >= altitude || item.maxAltitude <= altitude) continue;
      var mag = magnitude;
      var min = item.minDistance;
      if (min > 0 && item.wiggleDistanceWidth > 0f)
        min += Mathf.Sin(worldAngle * item.wiggleDistanceLength);
      else if (min == 0f)
        min = -0.1f; // To handle the center (0,0) correctly.
      var max = item.maxDistance;
      if (item.centerX != 0f || item.centerY != 0f)
      {
        mag = DUtils.Length(sx - item.centerX, sy - item.centerY);
      }
      var distOk = mag > min && (max >= radius || mag < max);
      if (!distOk) continue;
      if (CheckAngles)
      {
        min = item.minSector;
        max = item.maxSector;
        if (min != 0f || max != 1f)
        {
          var angle = baseAngle;
          if (item.wiggleSectorWidth > 0f)
            angle += Mathf.Sin(magnitude / item.wiggleSectorLength) * item.wiggleSectorWidth;
          if (angle < 0f) angle += 1f;
          if (angle >= 1f) angle -= 1f;
          var angleOk = min > max ? (angle >= min || angle < max) : angle >= min && angle < max;
          if (!angleOk) continue;
        }
      }
      var seed = item.seed ?? GetOffset(obj, item.biomeSeed);
      if (item.amount < 1f && Mathf.PerlinNoise((seed + bx / item.stretch) * 0.001f, (seed + by / item.stretch) * 0.001f) <= 1 - item.amount) continue;
      return item;
    }
    return null;
  }
}