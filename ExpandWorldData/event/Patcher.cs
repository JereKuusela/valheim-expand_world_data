using HarmonyLib;
using ExpandWorldData;
using System.Linq;

namespace ExpandWorld.Event;

public static class Patcher
{
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
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZNet), nameof(ZNet.Awake), typeof(Manager), nameof(Manager.DelayClientLoad), HarmonyPatchType.Prefix, Priority.Normal);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(Manager), nameof(Manager.InitializeServerData), HarmonyPatchType.Postfix, Priority.Last);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(Manager), nameof(Manager.InitializeClientData), HarmonyPatchType.Postfix, Priority.Normal);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.Awake), typeof(Manager), nameof(Manager.ApplyTiming), HarmonyPatchType.Postfix, Priority.Normal);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(Loader), nameof(Loader.ResolveEventConfiguration), HarmonyPatchType.Prefix, Priority.First);
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
    var wasPatched = ExpandWorldData.Patches.IsRegistered(typeof(MultipleEvents), nameof(MultipleEvents.UpdateEvents));
    if (!shouldPatch && wasPatched)
    {
      foreach (var entry in MultipleEvents.Events.ToList()) entry.Event.OnStop();
      MultipleEvents.Events.Clear();
    }
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.FixedUpdate), typeof(MultipleEvents), nameof(MultipleEvents.UpdateEvents), HarmonyPatchType.Prefix, Priority.Normal);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SetRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SetEvent), HarmonyPatchType.Prefix, Priority.Normal);
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.SendCurrentRandomEvent), typeof(MultipleEvents), nameof(MultipleEvents.SendEvent), HarmonyPatchType.Prefix, Priority.Normal);
  }

  private static void PatchCheckPerPlayer(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Apply(harmony, shouldPatch, typeof(RandEventSystem), nameof(RandEventSystem.UpdateRandomEvent), typeof(CheckPerPlayer), nameof(CheckPerPlayer.UpdateEvents), HarmonyPatchType.Prefix);
  }

}