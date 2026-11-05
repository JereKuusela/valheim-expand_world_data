using Common;
using ExpandWorldData.Data;
using HarmonyLib;
using System;
using System.Linq;

namespace ExpandWorldData;

public static class Patcher
{
  private static readonly Harmony Gameplay = new(EWD.GUID + ".gameplay");
  private static Type[] GameplayTypes = [];
  private static bool fixedActive;
  internal static bool GameplayInactive => !fixedActive && !Harmony.HasAnyPatches(EWD.GUID + ".gameplay") && !Patches.HasGameplay;

  public static void Initialize(Harmony harmony)
  {
    // Game only reserves 10 slots but alt biomes can use up to 32 biome indices.
    ResizeTempBiomeWeights();
    var types = typeof(EWD).Assembly.GetTypes().Where(type => type.IsDefined(typeof(HarmonyPatch), false)).ToArray();
    bool Permanent(Type type) => type.Namespace == "ServerSync" || type.Namespace == "Common" ||
      type.Namespace == "ExpandWorldData.SessionHooks" || type == typeof(InitializeWorld) ||
      type == typeof(InitializeRooms) || type == typeof(InitializeContent) || type == typeof(UpdatePatchesForRole) ||
      type.Namespace == "ExpandWorldData.Compatibility" && type != typeof(Compatibility.Pregenerate);
    foreach (var type in types.Where(Permanent)) harmony.CreateClassProcessor(type).Patch();
    GameplayTypes = types.Where(type => !Permanent(type)).ToArray();
    Update(harmony);
  }

  internal static bool SetGameplay(bool active)
  {
    if (active && !fixedActive)
    {
      fixedActive = true;
      foreach (var type in GameplayTypes) Gameplay.CreateClassProcessor(type).Patch();
    }
    else if (!active && fixedActive)
    {
      Gameplay.UnpatchSelf();
      fixedActive = false;
    }
    Update(EWD.Harmony);
    return active || GameplayInactive;
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
    Event.Patcher.Patch(harmony);
    Spawn.Patcher.Patch(harmony);
    Drop.Patcher.Patch(harmony);
    Features.Patcher.Patch(harmony);
    World.Patcher.Patch(harmony);
    Vegetation.Patcher.Patch(harmony);
    DataPatcher.Patch(harmony);
  }
}

// Server role is only known once ZNet exists.
[HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
public class UpdatePatchesForRole
{
  static void Postfix(ZNet __instance)
  {
    Session.Local(__instance);
    Refresh.Apply(Regen.Patches);
  }
}