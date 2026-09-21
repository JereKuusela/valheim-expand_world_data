using HarmonyLib;

namespace ExpandWorldData;

public static class Patcher
{
  public static void Initialize(Harmony harmony)
  {
    // Game only reserves 10 slots but alt biomes can use up to 32 biome indices.
    ResizeTempBiomeWeights();
    harmony.PatchAll();
    Update(harmony);
  }

  private static void ResizeTempBiomeWeights()
  {
    var field = AccessTools.Field(typeof(Heightmap), "s_tempBiomeWeights");
    if (field == null) return;
    var current = (float[])field.GetValue(null);
    if (current.Length >= 32) return;
    field.SetValue(null, new float[32]);
  }

  public static void Update(Harmony harmony)
  {
    if (harmony == null) return;
    ExpandWorld.Event.Patcher.Patch(harmony);
    ExpandWorld.Spawn.Patcher.Patch(harmony);
    Features.Patcher.Patch(harmony);
    Vegetation.Patcher.Patch(harmony);
    DataPatcher.Patch(harmony);
  }
}