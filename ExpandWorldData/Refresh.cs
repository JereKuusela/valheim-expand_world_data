namespace ExpandWorldData;

// Coalesces refresh requests so multiple data reloads trigger one patch update and one world regeneration.
public static class Refresh
{
  private const float WorldDelay = 1f;
  private static bool PatchesPending;
  private static float WorldTimer = -1f;

  public static void Patches() => PatchesPending = true;

  // World regeneration always refreshes patches first.
  public static void World()
  {
    PatchesPending = true;
    // Nothing to regenerate because the world hasn't been generated yet.
    if (WorldGenerator.instance?.m_world?.m_menu != false) return;
    // Debounced for smooth config editing.
    WorldTimer = WorldDelay;
  }

  // For call sites where patched code runs synchronously after the data change.
  public static void PatchesNow()
  {
    PatchesPending = false;
    Patcher.Update(EWD.Harmony);
  }

  public static void WorldNow()
  {
    WorldTimer = -1f;
    WorldInfo.AutomaticRegenerate();
  }
  public static void WorldStart()
  {
    WorldTimer = -1f;
    FlushPatches();
    if (WorldGenerator.instance == null) return;
    WorldGenerator.s_cachedBiomeAreas.Clear();
    WorldGenerator.s_cachedBiomes.Clear();
    foreach (var altBiome in AltBiomeList.m_altBiomes)
      altBiome.Sectors.Clear();
    WorldGenerator.instance.Pregenerate();
    AltBiomeWorldData.VerifyBiomeData(WorldGenerator.instance.m_world);
  }

  internal static void FlushPatches()
  {
    if (PatchesPending) PatchesNow();
  }

  internal static void Tick(float deltaTime)
  {
    FlushPatches();
    if (WorldTimer < 0f) return;
    WorldTimer -= deltaTime;
    if (WorldTimer <= 0f) WorldNow();
  }
}
