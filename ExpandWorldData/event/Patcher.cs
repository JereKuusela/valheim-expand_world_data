using HarmonyLib;
using ExpandWorldData;
using System.Linq;

namespace ExpandWorld.Event;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var isServer = Helper.IsServer();
    var hasData = EventManager.HasData;
    PatchMultipleEvents(harmony, isServer && Configuration.MultipleEvents);
    PatchCheckPerPlayer(harmony, isServer && Configuration.CheckPerPlayer);
    PatchLifecycle(harmony, isServer, hasData);
    PatchExtraChecks(harmony, hasData && Loader.ExtraData.Values.Any(data =>
      data.RequiredEnvironments.Count > 0 || data.PlayerLimit != null || data.EventLimit != null));
    PatchCheckBase(harmony, hasData && Loader.ExtraData.Values.Any(data =>
      data.MinBaseValue != 3 || data.MaxBaseValue != int.MaxValue));
    PatchCommands(harmony, isServer && hasData && Loader.ExtraData.Values.Any(data =>
      data.StartCommands?.Length > 0 || data.EndCommands?.Length > 0));
  }

  private static void PatchLifecycle(Harmony harmony, bool isServer, bool hasData)
  {
    // Role is not known yet at ZNet.Awake prefix.
    ExpandWorldData.Patches.Apply(harmony, true, typeof(ZNet), nameof(ZNet.Awake), typeof(EventManager), nameof(EventManager.DelayClientLoad), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, isServer, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(EventManager), nameof(EventManager.InitializeServerData), HarmonyPatchType.Postfix, Priority.Last);
    ExpandWorldData.Patches.Apply(harmony, !isServer, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(EventManager), nameof(EventManager.InitializeClientData), HarmonyPatchType.Postfix);
    ExpandWorldData.Patches.Apply(harmony, true, typeof(RandEventSystem), nameof(RandEventSystem.Awake), typeof(EventManager), nameof(EventManager.ApplyTiming), HarmonyPatchType.Postfix);
    ExpandWorldData.Patches.Apply(harmony, hasData, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(Loader), nameof(Loader.ResolveEventConfiguration), HarmonyPatchType.Prefix, Priority.First);
  }

  private static void PatchExtraChecks(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.InValidBiome), typeof(ExtraChecks), nameof(ExtraChecks.ValidateEventRequirements), HarmonyPatchType.Postfix);
  }

  private static void PatchCheckBase(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.CheckBase), typeof(ExtraChecks), nameof(ExtraChecks.ValidateBaseValue), HarmonyPatchType.Prefix);
  }

  private static void PatchCommands(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStart), typeof(Commands), nameof(Commands.RunStartCommands), HarmonyPatchType.Postfix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStop), typeof(Commands), nameof(Commands.RunEndCommands), HarmonyPatchType.Postfix);
  }

  private static void PatchMultipleEvents(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate), typeof(MultipleEvents), nameof(MultipleEvents.UpdateEvents), HarmonyPatchType.Prefix, onUnpatch: StopEvents);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SetEvent), HarmonyPatchType.Prefix);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SendCurrentRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SendEvent), HarmonyPatchType.Prefix);
  }

  private static void StopEvents()
  {
    foreach (var entry in MultipleEvents.Events.ToList()) entry.Event.OnStop();
    MultipleEvents.Events.Clear();
  }

  private static void PatchCheckPerPlayer(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.UpdateRandomEvent), typeof(CheckPerPlayer), nameof(CheckPerPlayer.UpdateEvents), HarmonyPatchType.Prefix);
  }

}