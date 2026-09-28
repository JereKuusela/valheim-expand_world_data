using System;
using System.Collections.Generic;
using Service;

namespace ExpandWorldData;

public static class AltBiomePlacement
{
  private static readonly Dictionary<BiomeSector, List<AltBiome>> Forced = [];
  private static readonly HashSet<string> Warned = new(StringComparer.OrdinalIgnoreCase);

  internal static void PrepareWorldAltBiomes(AltBiomeWorldData __instance)
  {
    Forced.Clear();
    var wg = WorldGenerator.instance;
    if (wg == null) return;
    Dictionary<string, AltBiome> altBiomes = new(StringComparer.OrdinalIgnoreCase);
    foreach (var alt in AltBiomeList.m_altBiomes)
      altBiomes[alt.m_name] = alt;
    foreach (var biome in __instance.Biomes.Values)
    {
      foreach (var sector in biome.Sectors)
      {
        var entry = BiomeCalculator.GetAltBiomeEntry(wg, sector.Center.x, sector.Center.y);
        if (entry == null) continue;
        List<AltBiome> list = [];
        foreach (var name in entry.altBiomes)
        {
          if (altBiomes.TryGetValue(name, out var alt))
            list.Add(alt);
          else if (Warned.Add(name))
            Log.Warning($"World data: Alt biome {name} not found.");
        }
        if (list.Count > 0)
          Forced[sector] = list;
      }
    }
  }

  internal static bool BlockForcedSectors(BiomeSector __instance, ref bool __result)
  {
    if (!Forced.ContainsKey(__instance)) return true;
    __result = false;
    return false;
  }

  // Applied after vanilla placement so that forced sectors don't count towards the caps.
  internal static void ApplyWorldAltBiomes()
  {
    foreach (var kvp in Forced)
    {
      foreach (var alt in kvp.Value)
        kvp.Key.AddModifier(alt);
    }
    Forced.Clear();
  }
}
