using System;

namespace ExpandWorldData;

[Flags]
public enum Regen
{
  None = 0,
  // Cached values and dynamic patches.
  Patches = 1,
  // Biome lookup caches and world generator data.
  Biomes = 2,
  // Heightmaps (also resets clutter).
  Terrain = 4,
  Clutter = 8,
  Water = 16,
  Minimap = 32,
  World = Biomes | Terrain | Clutter | Water | Minimap,
}

// Coalesces refresh requests so multiple data reloads are applied together after a delay.
public static class Refresh
{
  private const float Delay = 1f;
  private static Regen Pending = Regen.None;
  private static float Timer = -1f;

  public static void Request(Regen target)
  {
    // All data affects patches.
    Pending |= Regen.Patches | target;
    // Debounced for smooth config editing.
    Timer = Delay;
  }

  // For call sites where patched code runs synchronously after the data change.
  public static void PatchesNow()
  {
    Pending &= ~Regen.Patches;
    Patcher.Update(EWD.Harmony);
  }

  public static void WorldStart()
  {
    FlushPatches();
    Pending = Regen.None;
    Timer = -1f;
    WorldInfo.RegenerateBiomes();
  }

  internal static void FlushPatches()
  {
    if (Pending.HasFlag(Regen.Patches)) PatchesNow();
  }

  internal static void Tick(float deltaTime)
  {
    if (Timer < 0f) return;
    Timer -= deltaTime;
    if (Timer > 0f) return;
    Timer = -1f;
    var target = Pending;
    Pending = Regen.None;
    Execute(target);
  }

  private static void Execute(Regen target)
  {
    if (target.HasFlag(Regen.Patches)) Patcher.Update(EWD.Harmony);
    // Nothing to regenerate because the world hasn't been generated yet.
    if (WorldGenerator.instance?.m_world?.m_menu != false) return;
    if (target.HasFlag(Regen.Biomes)) WorldInfo.RegenerateBiomes();
    if (target.HasFlag(Regen.Terrain)) WorldInfo.RegenerateTerrain();
    if (target.HasFlag(Regen.Water)) WaterColor.Regenerate();
    if (target.HasFlag(Regen.Clutter) || target.HasFlag(Regen.Terrain)) ClutterSystem.instance?.ClearAll();
    if (target.HasFlag(Regen.Minimap)) WorldInfo.RegenerateMap();
  }
}
