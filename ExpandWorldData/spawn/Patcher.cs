using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;

namespace ExpandWorld.Spawn;

public static class Patcher
{
  public static void Patch(Harmony harmony)
  {
    var active = ActiveSpawns().ToHashSet();
    PatchLifecycle(harmony);
    PatchData(harmony, active.Any(Loader.Data.ContainsKey));
    PatchObjects(harmony, active.Any(Loader.Objects.ContainsKey));
    PatchRequirements(harmony, active.Select(spawn => Loader.Requirements.TryGetValue(spawn, out var requirements) ? requirements : null).Where(requirements => requirements != null).ToList()!);
    PatchGlobalKeys(harmony, SpawnManager.Override?.Any(UsesNumericGlobalKey) == true || active.Any(spawn => Loader.Requirements.TryGetValue(spawn, out var requirements) && Requirements.UsesNumericKey(requirements)));
  }

  private static void PatchRequirements(Harmony harmony, List<Requirements> requirements)
  {
    ExpandWorldData.Patches.Transpiler(harmony, requirements.Any(item => item.HasKeys), typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList), typeof(Requirements), nameof(Requirements.TranspileKeys));
    ExpandWorldData.Patches.Transpiler(harmony, requirements.Any(item => item.HasEnvironments), typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList), typeof(Requirements), nameof(Requirements.TranspileEnvironments));
    ExpandWorldData.Patches.Postfix(harmony, requirements.Any(item => item.HasLocations), typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood), typeof(Requirements), nameof(Requirements.CheckLocations));
    ExpandWorldData.Patches.Postfix(harmony, requirements.Any(item => item.HasObjects), typeof(SpawnSystem), nameof(SpawnSystem.IsSpawnPointGood), typeof(Requirements), nameof(Requirements.CheckObjects));
  }

  private static IEnumerable<SpawnSystem.SpawnData> ActiveSpawns()
  {
    if (SpawnManager.Override != null)
      foreach (var spawn in SpawnManager.Override) yield return spawn;
    if (Event.EventManager.HasData && RandEventSystem.instance != null)
      foreach (var spawn in RandEventSystem.instance.m_events.SelectMany(entry => entry.m_spawn)) yield return spawn;
    foreach (var spawn in AltBiomeManager.ActiveSpawns) yield return spawn;
  }

  private static bool UsesNumericGlobalKey(SpawnSystem.SpawnData spawn)
  {
    if (string.IsNullOrWhiteSpace(spawn.m_requiredGlobalKey)) return false;
    var split = spawn.m_requiredGlobalKey.Trim().Split(' ');
    return split.Length > 1 && int.TryParse(split[1], out _);
  }

  private static void PatchLifecycle(Harmony harmony)
  {
    ExpandWorldData.Patches.Postfix(harmony, true, typeof(ZoneSystem), nameof(ZoneSystem.Start), typeof(SpawnManager), nameof(SpawnManager.InitializeData), priority: Priority.VeryLow);
    ExpandWorldData.Patches.Postfix(harmony, true, typeof(SpawnSystem), nameof(SpawnSystem.Awake), typeof(SpawnManager), nameof(SpawnManager.InitializeSpawnSystem));
  }

  private static void PatchData(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.ApplyData));
  }

  private static void PatchObjects(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Postfix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(Loader), nameof(Loader.SpawnObjects));
  }

  private static void PatchGlobalKeys(Harmony harmony, bool shouldPatch)
  {
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.Mutate));
    ExpandWorldData.Patches.Prefix(harmony, shouldPatch, typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), typeof(GlobalKeys), nameof(GlobalKeys.CheckRequirement), argumentTypes: [typeof(string)]);
    ExpandWorldData.Patches.Postfix(harmony, shouldPatch, typeof(SpawnSystem), nameof(SpawnSystem.Spawn), typeof(GlobalKeys), nameof(GlobalKeys.Consume));
  }

}