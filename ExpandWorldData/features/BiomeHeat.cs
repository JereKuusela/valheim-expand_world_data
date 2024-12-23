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

  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetLava)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> GetLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeLava(instructions);

  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.IsLava)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> IsLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeLava(instructions);

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
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasHeat).operand)
    .SetOpcodeAndAdvance(OpCodes.Brfalse)
    .InstructionEnumeration();

  private static IEnumerable<CodeInstruction> PatchBiomeLava(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasHeat).operand)
    .SetOpcodeAndAdvance(OpCodes.Brfalse)
    .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Heightmap), nameof(Heightmap.IsBiomeEdge))))
    .Advance(-1)
    .SetAndAdvance(OpCodes.Ldc_I4_0, null)
    .SetAndAdvance(OpCodes.Nop, null)
    .InstructionEnumeration();

  /*
    [HarmonyPatch(typeof(CinderSpawner), nameof(CinderSpawner.CanSpawnCinder), typeof(Transform), typeof(Heightmap.Biome)), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> CanSpawnCinder(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat2(instructions);
    */

  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsLavaPreHeightmap)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> IsLavaPreHeightmap(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat2(instructions);

  private static IEnumerable<CodeInstruction> PatchBiomeHeat2(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasHeat).operand)
    .SetOpcodeAndAdvance(OpCodes.Brtrue)
    .InstructionEnumeration();

  private static bool HasHeat(Heightmap.Biome biome) => BiomeManager.TryGetData(biome, out var data) && data.lava;



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