using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))]
public class BiomeHeight
{
  static void Prefix(WorldGenerator __instance, ref Heightmap.Biome biome, ref Heightmap.Biome __state)
  {
    if (__instance.m_world.m_menu) return;
    __state = biome;
    biome = BiomeManager.GetTerrain(biome);
  }
  static void Postfix(WorldGenerator __instance, Heightmap.Biome __state, Heightmap.Biome biome, float wx, float wy, ref Color mask, ref float __result)
  {
    // TODO: Add patch for minimap generation and modify height while there.
    //if (BiomeManager.TryGetData(biome, out var data))
    //              biomeHeight = Configuration.WaterLevel + (biomeHeight - Configuration.WaterLevel) * data.mapColorMultiplier;
    if (__instance.m_world.m_menu) return;
    if (BiomeManager.TryGetColor(__state, out var color))
      mask = color;
    else if (__state == Heightmap.Biome.AshLands && __state != biome)
      __instance.GetAshlandsHeight(wx, wy, out mask);
    else if (__state == Heightmap.Biome.Mistlands && __state != biome)
      __instance.GetMistlandsHeight(wx, wy, out mask);

    if (BiomeManager.TryGetData(__state, out var data))
    {
      __result -= WorldInfo.WaterLevel;
      __result *= data.altitudeMultiplier;
      __result += data.altitudeDelta;
      if (__result < 0f)
      {
        __result *= data.waterDepthMultiplier;
      }
      if (__result > data.maximumAltitude)
        __result = data.maximumAltitude + Mathf.Pow(__result - data.maximumAltitude, data.excessFactor);
      if (__result < data.minimumAltitude)
        __result = data.minimumAltitude - Mathf.Pow(data.minimumAltitude - __result, data.excessFactor);
      __result += WorldInfo.WaterLevel;
    }
  }
}

public class GetAshlandsHeight
{
  private static readonly double DefaultWidthRestriction = 7500f;
  private static double WidthRestriction = DefaultWidthRestriction;
  private static readonly double DefaultLengthRestriction = 1000f;
  private static double LengthRestriction = DefaultLengthRestriction;
  public static void Patch(Harmony harmony, double widthRestriction, double lengthRestriction)
  {
    if (WidthRestriction == widthRestriction && LengthRestriction == lengthRestriction) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
    var transpiler = AccessTools.Method(typeof(GetAshlandsHeight), nameof(Transpiler));
    WidthRestriction = widthRestriction;
    LengthRestriction = lengthRestriction;
    harmony.Unpatch(method, transpiler);
    if (WidthRestriction != DefaultWidthRestriction || LengthRestriction != DefaultLengthRestriction)
      harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
  }

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 1000.0))
      .SetOperandAndAdvance(LengthRestriction)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 7500.0))
      .SetOperandAndAdvance(WidthRestriction)
      .InstructionEnumeration();
  }
}

public class CreateAshlandsGap
{
  private static bool IsPatched = false;
  public static void Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    else
      harmony.Unpatch(method, prefix);
  }

  static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}

public class CreateDeepNorthGap
{
  private static bool IsPatched = false;
  public static void Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    else
      harmony.Unpatch(method, prefix);
  }

  static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}
