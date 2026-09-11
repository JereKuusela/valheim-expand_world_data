using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData;

[HarmonyPatch]
public class ValidateAltBiomeCache
{
  static MethodBase TargetMethod() =>
    AccessTools.Method(typeof(AltBiomeWorldData), "TryLoadCache", [typeof(World)]);

  static void Postfix(World world, ref bool __result)
  {
    if (!__result || !Configuration.DataWorld || BiomeCalculator.BiomeData == null)
      return;
    var data = world.m_biomeData;
    var generator = WorldGenerator.instance;
    if (data == null || generator == null || !data.PointsGenerated)
      return;

    var step = Math.Max(1, data.Size / 64);
    var start = step / 2;
    for (var y = start; y < data.Size; y += step)
    {
      for (var x = start; x < data.Size; x += step)
      {
        var wx = AltBiomeWorldData.MapSpaceToWorldSpace(x);
        var wy = AltBiomeWorldData.MapSpaceToWorldSpace(y);
        var cached = BiomeHelpers.ToBiome(data.PointBiomes[x, y]);
        var current = WorldManager.UseNativeGeneration
          ? generator.GetBiome(wx, wy)
          : BiomeCalculator.Get(generator, wx, wy);
        if (cached == current)
          continue;

        Log.Info($"Cached biome sectors do not match the loaded world data ({cached} -> {current}). Rebuilding them.");
        world.m_biomeData = null!;
        __result = false;
        return;
      }
    }
  }
}

[HarmonyPatch]
public class GuaranteeMinimumAltBiomes
{
  private class OriginalLimits
  {
    public AltBiome AltBiome = null!;
    public int MaxAmount;
    public int MinEdge;
    public int MaxEdge;
    public float MinHeight;
    public float MaxHeight;
  }

  static MethodBase TargetMethod() =>
    AccessTools.Method(typeof(AltBiomeWorldData), "GenerateAltBiomes");

  static void Prefix(AltBiomeWorldData __instance, out List<OriginalLimits> __state)
  {
    __state = [];
    foreach (var altBiome in AltBiomeList.m_altBiomes)
    {
      if (!altBiome.m_enabled || altBiome.m_minAmountSpawned <= altBiome.Sectors.Count)
        continue;

      var candidates = __instance.Biomes
        .Where(entry => entry.Key != Heightmap.Biome.None && altBiome.m_biome.HasFlag(entry.Key))
        .SelectMany(entry => entry.Value.Sectors)
        .Distinct()
        .ToList();
      var required = altBiome.m_minAmountSpawned - altBiome.Sectors.Count;
      if (candidates.Count(sector => sector.CanAddModifier(altBiome)) >= required)
        continue;

      var original = new OriginalLimits
      {
        AltBiome = altBiome,
        MaxAmount = altBiome.m_maxAmountSpawned,
        MinEdge = altBiome.m_minEdgeSize,
        MaxEdge = altBiome.m_maxEdgeSize,
        MinHeight = altBiome.m_minAvgHeight,
        MaxHeight = altBiome.m_maxAvgHeight
      };
      altBiome.m_maxAmountSpawned = altBiome.m_minAmountSpawned;
      altBiome.m_minEdgeSize = 0;
      altBiome.m_maxEdgeSize = int.MaxValue;
      altBiome.m_minAvgHeight = float.MinValue;
      altBiome.m_maxAvgHeight = float.MaxValue;

      var relaxed = candidates.Count(sector => sector.CanAddModifier(altBiome));
      if (relaxed == 0)
      {
        Restore(original);
        continue;
      }

      __state.Add(original);
      Log.Info($"Alt biome '{altBiome.m_name}' has too few sectors matching its size and height limits. Relaxing those limits for its minimum placement.");
    }
  }

  static Exception? Finalizer(Exception? __exception, List<OriginalLimits> __state)
  {
    foreach (var original in __state)
      Restore(original);
    return __exception;
  }

  private static void Restore(OriginalLimits original)
  {
    original.AltBiome.m_maxAmountSpawned = original.MaxAmount;
    original.AltBiome.m_minEdgeSize = original.MinEdge;
    original.AltBiome.m_maxEdgeSize = original.MaxEdge;
    original.AltBiome.m_minAvgHeight = original.MinHeight;
    original.AltBiome.m_maxAvgHeight = original.MaxHeight;
  }
}
