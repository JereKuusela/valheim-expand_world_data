using HarmonyLib;

namespace ExpandWorldData;

public static class Patcher
{
  public static void Initialize(Harmony harmony)
  {
    harmony.PatchAll();
    Update(harmony);
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