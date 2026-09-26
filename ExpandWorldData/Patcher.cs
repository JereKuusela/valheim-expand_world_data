using HarmonyLib;

namespace ExpandWorldData;

public static class Patcher
{
  public static bool DataEnvironments;
  public static bool DataBiome;
  public static bool DataTerritory;
  public static bool DataWorld;
  public static bool DataClutter;
  public static bool DataAltBiomes;
  public static bool DataEvents;
  public static bool DataSpawns;

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
    ExpandWorld.Drops.Patcher.Patch(harmony);
    Features.Patcher.Patch(harmony);
    World.Patcher.Patch(harmony);
    Vegetation.Patcher.Patch(harmony);
    DataPatcher.Patch(harmony);
  }
}