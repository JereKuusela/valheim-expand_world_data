using System.Collections.Generic;
using System.Reflection.Emit;
using ExpandWorldData.Biome;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData.Features;

public class BiomeHeat
{
  // Vanilla code has bug that heat doesn't reset when not in hot biome.
  internal static IEnumerable<CodeInstruction> FixLavaHeatReset(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
  {
    var matcher = new CodeMatcher(instructions, generator)
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands))));
    if (matcher.IsInvalid)
    {
      Log.Warning("Character.UpdateLava: IsAshlands call not found, skipping the lava heat reset fix.");
      return matcher.InstructionEnumeration();
    }
    // Result becomes HasLava(m_lastBiome) || (false && IsAshlands). The call stays in the IL but is always jumped over.
    var skipCall = generator.DefineLabel();
    var afterCall = generator.DefineLabel();
    return matcher
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse, skipCall))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Br, afterCall))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Pop).WithLabels(skipCall))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Pop))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0).WithLabels(afterCall))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Character), nameof(Character.m_lastBiome))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BiomeHeat), nameof(HasLava))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Or))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, 0f))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Character), nameof(Character.m_lavaHeatLevel))))
      .InstructionEnumeration();
  }

  internal static IEnumerable<CodeInstruction> UpdateLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  internal static bool ReplaceGetLava(Heightmap __instance, Vector3 worldPos, ref float __result)
  {
    __result = GetLava(__instance, worldPos);
    return false;
  }
  internal static bool ReplaceIsLava(Heightmap __instance, Vector3 worldPos, float lavaValue, ref bool __result)
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
    hm.WorldToVertexMask(pos, out var x, out var y);
    var index = y + x * (hm.m_width + 1);
    var colors = hm.m_renderMesh.colors32;
    // Some user got index out of bounds. Not sure why but this should fix that.
    if (index < 0 || index >= colors.Length) return hm.GetVegetationMask(pos);
    var color = colors[index];
    // 0.92 is still barely visible so safer to use 0.93 (* 255).
    if (color.r < 237 || color.a < 237) return 0f;
    // Multiplier used to lower damage on barely visible lava.
    // 0.5 ensures that there is always some damage.
    var multiplier = color.a == 255 ? 1f : 0.5f + (color.a - 237) / 36;
    return hm.GetVegetationMask(pos) * multiplier;
  }


  internal static IEnumerable<CodeInstruction> GetHeightOffset(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  internal static IEnumerable<CodeInstruction> ScanForLava(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  internal static IEnumerable<CodeInstruction> UpdateLavaAmbient(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);

  internal static IEnumerable<CodeInstruction> UpdateLavaAmbientLoops(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat(instructions);



  private static IEnumerable<CodeInstruction> PatchBiomeHeat(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasLava).operand)
    .SetOpcodeAndAdvance(OpCodes.Brfalse)
    .InstructionEnumeration();

  internal static IEnumerable<CodeInstruction> IsLavaPreHeightmap(IEnumerable<CodeInstruction> instructions) => PatchBiomeHeat2(instructions);

  private static IEnumerable<CodeInstruction> PatchBiomeHeat2(IEnumerable<CodeInstruction> instructions) =>
    new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32))
    .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(HasLava).operand)
    .SetOpcodeAndAdvance(OpCodes.Brtrue)
    .InstructionEnumeration();

  private static bool HasLava(Heightmap.Biome biome) => (biome & BiomeManager.LavaBiomes) != 0;
  private static bool HasFullLava(Heightmap.Biome biome) => (biome & BiomeManager.FullLavaBiomes) != 0;



  internal static bool IsAshlands(float x, float y, ref bool __result)
  {
    var wg = WorldGenerator.instance;
    if (wg == null || wg.m_world.m_menu) return true;
    __result = BiomeCalculator.IsBoiling(wg, x, y);
    return false;
  }
}