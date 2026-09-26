using HarmonyLib;

namespace ExpandWorldData.Features;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    GetAshlandsHeight.Patch(harmony, Configuration.AshlandsWidthRestriction, Configuration.AshlandsLengthRestriction);
    CreateAshlandsGap.Patch(harmony, !Configuration.AshlandsGap);
    CreateDeepNorthGap.Patch(harmony, !Configuration.DeepNorthGap);
    PatchWaterColor(harmony);
  }

  private static void PatchWaterColor(Harmony harmony)
  {
    var shouldPatch = Configuration.CustomWaterColor;
    var callback = nameof(WaterColor.StartBiomeTransition);
    if (!shouldPatch && Patches.IsRegistered(typeof(WaterColor), callback)) WaterColor.StopTransition();
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.AddKnownBiome), typeof(WaterColor), nameof(WaterColor.StartBiomeTransition), HarmonyPatchType.Postfix);
    Patches.Apply(harmony, shouldPatch, typeof(Player), nameof(Player.OnSpawned), typeof(WaterColor), nameof(WaterColor.ResetTransition), HarmonyPatchType.Postfix);
  }
}