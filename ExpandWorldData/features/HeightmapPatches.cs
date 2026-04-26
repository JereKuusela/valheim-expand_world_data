using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace ExpandWorldData;


[HarmonyPatch(typeof(Heightmap))]
public class HeightmapPatches
{
  private static readonly Dictionary<Heightmap, Color32[]> cornerColors = [];

  static Color32[] InitCornerColors(Heightmap __instance)
  {
    var center = __instance.transform.position;
    var territory = BiomeCalculator.GetTerritory(center.x - 32f, center.z - 32f);
    var c0 = territory?.colorMap ?? Heightmap.GetBiomeColor(__instance.m_cornerBiomes[0]);
    territory = BiomeCalculator.GetTerritory(center.x + 32f, center.z - 32f);
    var c1 = territory?.colorMap ?? Heightmap.GetBiomeColor(__instance.m_cornerBiomes[1]);
    territory = BiomeCalculator.GetTerritory(center.x - 32f, center.z + 32f);
    var c2 = territory?.colorMap ?? Heightmap.GetBiomeColor(__instance.m_cornerBiomes[2]);
    territory = BiomeCalculator.GetTerritory(center.x + 32f, center.z + 32f);
    var c3 = territory?.colorMap ?? Heightmap.GetBiomeColor(__instance.m_cornerBiomes[3]);
    if (c0 == c1 && c0 == c2 && c0 == c3)
      cornerColors[__instance] = [c0];
    else
      cornerColors[__instance] = [c0, c1, c2, c3];
    return cornerColors[__instance];
  }
  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.OnDestroy)), HarmonyPostfix]
  static void RemoveCornerTerritories(Heightmap __instance)
  {
    cornerColors.Remove(__instance);
  }

  private static Vector3 CalcWorld(Heightmap __instance, float ix, float iy)
  {
    var center = __instance.transform.position;
    var x = center.x + (ix - 0.5f) * 64f;
    var z = center.z + (iy - 0.5f) * 64f;
    return new Vector3(x, 0, z);
  }

  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(float), typeof(float)), HarmonyPrefix]
  static bool CustomGetBiomeColor(Heightmap __instance, float ix, float iy, ref Color __result)
  {
    var pos = CalcWorld(__instance, ix, iy);
    var territory = BiomeCalculator.GetTerritory(pos.x, pos.z);
    if (territory == null || !territory.colorTerrain.HasValue)
      return true;

    __result = territory.colorTerrain.Value;
    return false;
  }

  // Blending didn't seem good for territories. People most likely want exact shapes.
  private static Color GetBiomeColorFunc(Heightmap hm, float ix, float iy)
  {
    if (!cornerColors.TryGetValue(hm, out var colors))
      colors = InitCornerColors(hm);

    if (colors.Length == 1)
      return colors[0];

    Color32 a = Color32.Lerp(colors[0], colors[1], ix);
    Color32 b = Color32.Lerp(colors[2], colors[3], ix);
    return Color32.Lerp(a, b, iy);
  }


  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), typeof(Heightmap.Biome)), HarmonyPrefix]
  static bool GetBiomeColor(Heightmap.Biome biome, ref Color32 __result)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return true;
    __result = data.colorTerrain;
    return false;
  }
}
