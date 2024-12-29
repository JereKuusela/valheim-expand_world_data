using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData;

[HarmonyPatch]
public class BiomeHeat
{
  [HarmonyPatch(typeof(Character), nameof(Character.UpdateLava)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> UpdateLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);


  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetLava)), HarmonyPrefix]
  static bool GetLava(Heightmap __instance, Vector3 worldPos, ref float __result)
  {
    __result = GetLava(__instance, worldPos);
    return false;
  }
  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.IsLava)), HarmonyPrefix]
  static bool IsLava(Heightmap __instance, Vector3 worldPos, float lavaValue, ref bool __result)
  {
    __result = GetLava(__instance, worldPos) > lavaValue;
    return false;
  }

  private static float GetLava(Heightmap hm, Vector3 pos)
  {
    var biome = hm.GetBiome(pos);
    if (!HasLava(biome)) return 0f;
    if (HasFullLava(biome) && !hm.IsBiomeEdge()) return hm.GetVegetationMask(pos);
    // Lava is only visible on Ashlands terrain (r and a channels higher than 0.92).
    // Biome edges blend the color which removes the lava texture.
    // So the terrain color must be checked to determine if lava would be visible.
    hm.WorldToVertex(pos, out var x, out var y);
    var index = y + x * (hm.m_width + 1);
    var color = hm.m_renderMesh.colors32[index];
    // 0.92 is still barely visible so safer to use 0.93 (* 255).
    if (color.r < 237 || color.a < 237) return 0f;
    // Multiplier used to lower damage on barely visible lava.
    // 0.5 ensures that there is always some damage.
    var multiplier = color.a == 255 ? 1f : 0.5f + (color.a - 237) / 36;
    return hm.GetVegetationMask(pos) * multiplier;
  }


  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetHeightOffset)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> GetHeightOffset(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.ScanForLava)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> ScanForLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbient)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> UpdateLavaAmbient(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.UpdateLavaAmbientLoops)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> UpdateLavaAmbientLoops(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);



  private static IEnumerable<CodeInstruction> PatchBiomeHeat(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasLava).operand)
    .SetOpcodeAndAdvance(OpCodes.Brfalse)
    .InstructionEnumeration();


  /*
    [HarmonyPatch(typeof(CinderSpawner), nameof(CinderSpawner.CanSpawnCinder), typeof(Transform), typeof(Heightmap.Biome)), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> CanSpawnCinder(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat2(instructions);
    */

  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsLavaPreHeightmap)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> IsLavaPreHeightmap(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat2(instructions);

  private static IEnumerable<CodeInstruction> PatchBiomeHeat2(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasLava).operand)
    .SetOpcodeAndAdvance(OpCodes.Brtrue)
    .InstructionEnumeration();

  private static bool HasLava(Heightmap.Biome biome) => (biome & BiomeManager.LavaBiomes) != 0;
  private static bool HasFullLava(Heightmap.Biome biome) => (biome & BiomeManager.FullLavaBiomes) != 0;



  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands)), HarmonyPrefix]
  static bool IsAshlands(float x, float y, ref bool __result)
  {
    var wg = WorldGenerator.instance;
    if (wg == null || wg.m_world.m_menu) return true;
    if (!Configuration.DataWorld) return true;
    var boiling = BiomeCalculator.GetBoiling(wg, x, y);
    __result = boiling > 0f;
    return false;
  }
}