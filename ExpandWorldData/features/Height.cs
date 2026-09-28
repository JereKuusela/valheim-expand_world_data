using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData;

public class BiomeHeight
{
  private static float ApplyHeight(float height, BiomeData data)
  {
    height *= data.altitudeMultiplier;
    height += data.altitudeDelta;
    if (height < 0f)
      height *= data.waterDepthMultiplier;
    if (height > data.maximumAltitude)
      height = data.maximumAltitude + data.excessSign * Mathf.Pow(height - data.maximumAltitude, data.excessFactor);
    if (height < data.minimumAltitude)
      height = data.minimumAltitude - data.excessSign * Mathf.Pow(data.minimumAltitude - height, data.excessFactor);
    return height;
  }

  private static float ApplyHeight(float height, TerritoryData data)
  {
    height *= data.altitudeMultiplier;
    height += data.altitudeDelta;
    if (height < 0f)
      height *= data.waterDepthMultiplier;
    if (height > data.maximumAltitude)
      height = data.maximumAltitude + data.excessSign * Mathf.Pow(height - data.maximumAltitude, data.excessFactor);
    if (height < data.minimumAltitude)
      height = data.minimumAltitude - data.excessSign * Mathf.Pow(data.minimumAltitude - height, data.excessFactor);
    return height;
  }

  internal static void ReplaceTerrain(WorldGenerator __instance, ref Heightmap.Biome biome, ref Heightmap.Biome __state)
  {
    if (__instance.m_world.m_menu) return;
    __state = biome;
    biome = BiomeManager.GetTerrain(biome);
  }
  [ThreadStatic]
  public static float LastX = 0f;
  [ThreadStatic]
  public static float LastY = 0f;
  internal static void ModifyHeight(WorldGenerator __instance, Heightmap.Biome __state, Heightmap.Biome biome, float wx, float wy, ref Color mask, ref float __result)
  {
    LastX = wx;
    LastY = wy;
    if (__instance.m_world.m_menu) return;
    if (BiomeManager.TryGetColor(__state, out var color))
      mask = color;
    else if (__state == Heightmap.Biome.AshLands && __state != biome)
      __instance.GetAshlandsHeight(wx, wy, out mask);
    else if (__state == Heightmap.Biome.Mistlands && __state != biome)
      __instance.GetMistlandsHeight(wx, wy, out mask);

    if (BiomeManager.TryGetData(__state, out var data))
    {
      if (data.lavaAmount != 1f)
      {
        // Biome seed wouldn't make sense because then lava intensity would match the biome shape.
        var seed = data.GetLavaSeed();
        var lava = Mathf.PerlinNoise(seed + wx * data.lavaStretch, seed + wy * data.lavaStretch);
        if (lava > data.lavaAmount)
          mask.a = 0;
        else
          mask.a = lava / data.lavaAmount;
        __result -= mask.a;
      }
    }

    var territoryEntry = BiomeCalculator.GetTerritoryEntry(__instance, wx, wy);
    if (territoryEntry != null && TerritoryManager.TryGetData(territoryEntry.territory, out var territoryColorData) && territoryColorData.colorTerrain.HasValue)
      mask = territoryColorData.colorTerrain.Value;

    __result -= WorldInfo.WaterLevel;
    if (BiomeManager.TryGetData(__state, out data))
      __result = ApplyHeight(__result, data);

    if (territoryEntry != null && TerritoryManager.TryGetData(territoryEntry.territory, out var territory))
      __result = ApplyHeight(__result, territory);

    __result += WorldInfo.WaterLevel;
  }
}

public class GetAshlandsHeight
{
  public const double DefaultWidthRestriction = 7500.0;
  public const double DefaultLengthRestriction = 1000.0;

  internal static IEnumerable<CodeInstruction> ReplaceRestrictions(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, DefaultLengthRestriction))
      .SetOperandAndAdvance(Configuration.AshlandsLengthRestriction)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, DefaultWidthRestriction))
      .SetOperandAndAdvance(Configuration.AshlandsWidthRestriction)
      .InstructionEnumeration();
  }
}

public class DisableGap
{
  internal static bool SkipGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}
