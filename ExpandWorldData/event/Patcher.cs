using HarmonyLib;
using ExpandWorldData;
using System.Linq;

namespace ExpandWorld.Event;

public static class Patcher
{
  private static bool LifecyclePatched;
  private static bool ExtraChecksPatched;
  private static bool CheckBasePatched;
  private static bool CommandsPatched;
  private static bool MultipleEventsPatched;
  private static bool CheckPerPlayerPatched;

  public static void Patch(Harmony harmony)
  {
    PatchMultipleEvents(harmony, Configuration.DataEvents && Configuration.MultipleEvents);
    PatchCheckPerPlayer(harmony, Configuration.DataEvents && Configuration.CheckPerPlayer);
    PatchLifecycle(harmony, Configuration.DataEvents);
    PatchExtraChecks(harmony, Configuration.DataEvents && Loader.ExtraData.Values.Any(data =>
      data.RequiredEnvironments.Count > 0 || data.PlayerLimit != null || data.EventLimit != null));
    PatchCheckBase(harmony, Configuration.DataEvents && Loader.ExtraData.Values.Any(data =>
      data.MinBaseValue != 3 || data.MaxBaseValue != int.MaxValue));
    PatchCommands(harmony, Configuration.DataEvents && Loader.ExtraData.Values.Any(data =>
      data.StartCommands?.Length > 0 || data.EndCommands?.Length > 0));
  }

  private static void PatchLifecycle(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == LifecyclePatched) return;
    var patches = new (System.Type Original, string OriginalName, System.Type Patch, string PatchName, HarmonyPatchType Type, int Priority)[]
    {
      (typeof(ZNet), nameof(ZNet.Awake), typeof(Manager), nameof(Manager.DelayClientLoad), HarmonyPatchType.Prefix, Priority.Normal),
      (typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(Manager), nameof(Manager.InitializeServerData), HarmonyPatchType.Postfix, Priority.Last),
      (typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(Manager), nameof(Manager.InitializeClientData), HarmonyPatchType.Postfix, Priority.Normal),
      (typeof(RandEventSystem), nameof(RandEventSystem.Awake), typeof(Manager), nameof(Manager.ApplyTiming), HarmonyPatchType.Postfix, Priority.Normal),
      (typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(Loader), nameof(Loader.ResolveEventConfiguration), HarmonyPatchType.Prefix, Priority.First),
    };
    SetPatches(harmony, shouldPatch, patches);
    LifecyclePatched = shouldPatch;
  }

  private static void PatchExtraChecks(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == ExtraChecksPatched) return;
    SetPatch(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.InValidBiome), typeof(ExtraChecks), nameof(ExtraChecks.ValidateEventRequirements), HarmonyPatchType.Postfix);
    ExtraChecksPatched = shouldPatch;
  }

  private static void PatchCheckBase(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == CheckBasePatched) return;
    SetPatch(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.CheckBase), typeof(ExtraChecks), nameof(ExtraChecks.ValidateBaseValue), HarmonyPatchType.Prefix);
    CheckBasePatched = shouldPatch;
  }

  private static void PatchCommands(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == CommandsPatched) return;
    SetPatch(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStart), typeof(Commands), nameof(Commands.RunStartCommands), HarmonyPatchType.Postfix);
    SetPatch(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStop), typeof(Commands), nameof(Commands.RunEndCommands), HarmonyPatchType.Postfix);
    CommandsPatched = shouldPatch;
  }

  private static void PatchMultipleEvents(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == MultipleEventsPatched) return;
    if (!shouldPatch)
    {
      foreach (var entry in MultipleEvents.Events.ToList()) entry.Event.OnStop();
      MultipleEvents.Events.Clear();
    }
    var patches = new (System.Type Original, string OriginalName, System.Type Patch, string PatchName, HarmonyPatchType Type, int Priority)[]
    {
      (typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate), typeof(MultipleEvents), nameof(MultipleEvents.UpdateEvents), HarmonyPatchType.Prefix, Priority.Normal),
      (typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SetEvent), HarmonyPatchType.Prefix, Priority.Normal),
      (typeof(RandEventSystem), nameof(RandEventSystem.SendCurrentRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SendEvent), HarmonyPatchType.Prefix, Priority.Normal),
    };
    SetPatches(harmony, shouldPatch, patches);
    MultipleEventsPatched = shouldPatch;
  }

  private static void PatchCheckPerPlayer(Harmony harmony, bool shouldPatch)
  {
    if (shouldPatch == CheckPerPlayerPatched) return;
    SetPatch(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.UpdateRandomEvent), typeof(CheckPerPlayer), nameof(CheckPerPlayer.UpdateEvents), HarmonyPatchType.Prefix);
    CheckPerPlayerPatched = shouldPatch;
  }

  private static void SetPatches(Harmony harmony, bool shouldPatch, (System.Type Original, string OriginalName, System.Type Patch, string PatchName, HarmonyPatchType Type, int Priority)[] patches)
  {
    foreach (var patch in patches)
      SetPatch(harmony, shouldPatch, patch.Original, patch.OriginalName, patch.Patch, patch.PatchName, patch.Type, patch.Priority);
  }

  private static void SetPatch(Harmony harmony, bool shouldPatch, System.Type originalType, string originalName, System.Type patchType, string patchName, HarmonyPatchType type, int priority = Priority.Normal)
  {
    var original = AccessTools.Method(originalType, originalName);
    var patch = AccessTools.Method(patchType, patchName);
    if (shouldPatch)
    {
      var harmonyPatch = new HarmonyMethod(patch) { priority = priority };
      if (type == HarmonyPatchType.Prefix) harmony.Patch(original, prefix: harmonyPatch);
      else harmony.Patch(original, postfix: harmonyPatch);
    }
    else harmony.Unpatch(original, patch);
  }
}