using HarmonyLib;

namespace ExpandWorldData;

[HarmonyPatch]
public class Spawner
{
  /*
  // Disabled until properly tested.
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
  */


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