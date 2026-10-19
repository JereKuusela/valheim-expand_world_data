using HarmonyLib;
using System.Linq;

namespace ExpandWorldData.Event;

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
      data.RequiredEnvironments != null || data.ForbiddenEnvironments != null || data.RequiredKeyRows != null || data.ForbiddenKeyRows != null || data.PlayerLimit != null || data.EventLimit != null));
    PatchCheckBase(harmony, hasData && Loader.ExtraData.Values.Any(data =>
      data.MinBaseValue != 3 || data.MaxBaseValue != int.MaxValue));
    PatchCommands(harmony, isServer && hasData && Loader.ExtraData.Values.Any(data =>
      data.StartCommands?.Length > 0 || data.EndCommands?.Length > 0));
  }

  private static void PatchLifecycle(Harmony harmony, bool isServer, bool hasData)
  {
    // Role is not known yet at ZNet.Awake prefix.
    Patches.Postfix(harmony, isServer, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(EventManager), nameof(EventManager.InitializeServerData), priority: Priority.Last);
    Patches.Postfix(harmony, true, typeof(RandEventSystem), nameof(RandEventSystem.Awake), typeof(EventManager), nameof(EventManager.ApplyTiming));
    Patches.Prefix(harmony, hasData, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(Loader), nameof(Loader.ResolveEventConfiguration), priority: Priority.First);
  }

  private static void PatchExtraChecks(Harmony harmony, bool shouldPatch)
  {
    Patches.Postfix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.InValidBiome), typeof(ExtraChecks), nameof(ExtraChecks.ValidateEventRequirements));
  }

  private static void PatchCheckBase(Harmony harmony, bool shouldPatch)
  {
    Patches.Prefix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.CheckBase), typeof(ExtraChecks), nameof(ExtraChecks.ValidateBaseValue));
  }

  private static void PatchCommands(Harmony harmony, bool shouldPatch)
  {
    Patches.Postfix(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStart), typeof(Commands), nameof(Commands.RunStartCommands));
    Patches.Postfix(harmony, shouldPatch, typeof(RandomEvent), nameof(RandomEvent.OnStop), typeof(Commands), nameof(Commands.RunEndCommands));
  }

  private static void PatchMultipleEvents(Harmony harmony, bool shouldPatch)
  {
    Patches.Prefix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate), typeof(MultipleEvents), nameof(MultipleEvents.UpdateEvents), onUnpatch: StopEvents);
    Patches.Prefix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SetEvent));
    Patches.Prefix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SendCurrentRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SendEvent));
  }

  private static void StopEvents()
  {
    foreach (var entry in MultipleEvents.Events.ToList()) entry.Event.OnStop();
    MultipleEvents.Events.Clear();
  }

  private static void PatchCheckPerPlayer(Harmony harmony, bool shouldPatch)
  {
    Patches.Prefix(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.UpdateRandomEvent), typeof(CheckPerPlayer), nameof(CheckPerPlayer.UpdateEvents));
  }

}