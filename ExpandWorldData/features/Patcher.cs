using HarmonyLib;

namespace ExpandWorldData.Features;

public static class Patcher
{
  private static bool WaterColorPatched;

  public static void Patch(Harmony harmony)
  {
    var shouldPatch = Configuration.CustomWaterColor;
    if (shouldPatch == WaterColorPatched) return;
    if (!shouldPatch) WaterColor.StopTransition();
    SetPatch(harmony, shouldPatch, typeof(Player), nameof(Player.AddKnownBiome), typeof(WaterColor), nameof(WaterColor.StartBiomeTransition));
    SetPatch(harmony, shouldPatch, typeof(Player), nameof(Player.OnSpawned), typeof(WaterColor), nameof(WaterColor.ResetTransition));
    WaterColorPatched = shouldPatch;
  }

  private static void SetPatch(Harmony harmony, bool shouldPatch, System.Type originalType, string originalName, System.Type patchType, string patchName)
  {
    var original = AccessTools.Method(originalType, originalName);
    var patch = AccessTools.Method(patchType, patchName);
    if (shouldPatch) harmony.Patch(original, postfix: new HarmonyMethod(patch));
    else harmony.Unpatch(original, patch);
  }
}