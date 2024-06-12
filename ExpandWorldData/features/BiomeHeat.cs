using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ExpandWorldData;

// Disabled until properly tested.
//[HarmonyPatch]
public class Spawner
{

  [HarmonyPatch(typeof(Character), nameof(Character.UpdateLava))]
  static IEnumerable<CodeInstruction> UpdateLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);
  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetLava))]
  static IEnumerable<CodeInstruction> GetLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);
  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.IsLava))]
  static IEnumerable<CodeInstruction> IsLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);
  [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetHeightOffset))]
  static IEnumerable<CodeInstruction> GetHeightOffset(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);


  private static IEnumerable<CodeInstruction> PatchBiomeHeat(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, 32))
    .SetAndAdvance(OpCodes.Call, 100)
    .SetOpcodeAndAdvance(OpCodes.Brfalse)
    .InstructionEnumeration();

  private static bool HasHeat(Heightmap.Biome biome) => BiomeManager.TryGetData(biome, out var data) && data.lava;



  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands)), HarmonyPrefix]
  static bool IsAshlands(float x, float y, ref bool __result)
  {
    var wg = WorldGenerator.instance;
    if (wg.m_world.m_menu) return true;
    if (!Configuration.DataWorld) return true;
    var biome = BiomeCalculator.Get(wg, x, y);
    __result = BiomeManager.TryGetData(biome, out var data) && data.lava;
    return false;
  }
}