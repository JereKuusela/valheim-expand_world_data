using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ExpandWorldData.Locations;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData.Spawn;

public sealed class NearGroup(DistanceGroup source, bool locations)
{
  public readonly bool Locations = locations;
  public readonly string[] Names = source.Names;
  public readonly float Distance = source.Distance;
  private HashSet<int>? Hashes;
  private int HashVersion = -1;

  // Resolved lazily because location groups and prefabs are not available when the data is loaded.
  public HashSet<int> Resolve()
  {
    if (Hashes != null && HashVersion == Requirements.Version) return Hashes;
    Hashes = [];
    foreach (var name in Names)
    {
      if (Locations)
      {
        Hashes.Add(name.GetStableHashCode());
        foreach (var hash in LocationClientData.GetGroupHashes(name)) Hashes.Add(hash);
      }
      else
      {
        foreach (var hash in global::Data.PrefabHelper.GetPrefabs(name, "")) Hashes.Add(hash);
      }
    }
    HashVersion = Requirements.Version;
    return Hashes;
  }
}

///<summary>Extra spawn requirements that don't fit the vanilla fields.</summary>
public sealed class Requirements
{
  // Changes when location groups change.
  public static int Version;
  private const string Never = "ewd_requirement_failed";
  private static readonly List<string> NeverList = [Never];
  private static readonly int ProxyHash = "LocationProxy".GetStableHashCode();
  private static readonly List<ZDO> Temp = [];
  private static readonly HashSet<ZoneSystem.SectorIndex> Visited = [];

  public List<string[]>? Keys;
  public List<string[]>? ForbiddenKeys;
  public List<string[]>? Environments;
  public List<string[]>? ForbiddenEnvironments;
  public List<NearGroup[]>? Locations;
  public List<NearGroup[]>? ForbiddenLocations;
  public List<NearGroup[]>? Objects;
  public List<NearGroup[]>? ForbiddenObjects;

  public bool HasKeys => Keys != null || ForbiddenKeys != null;
  public bool HasEnvironments => Environments != null || ForbiddenEnvironments != null;
  public bool HasLocations => Locations != null || ForbiddenLocations != null;
  public bool HasObjects => Objects != null || ForbiddenObjects != null;
  public bool IsEmpty => !HasKeys && !HasEnvironments && !HasLocations && !HasObjects;

  // Rows that only have a single plain entry are handled by the vanilla fields (see Loader.FromData).
  public static Requirements Create(Data data, string file, List<string[]>? keys, List<string[]>? environments)
  {
    return new()
    {
      Keys = keys,
      ForbiddenKeys = StringList.ToRows(data.forbiddenGlobalKey),
      Environments = environments,
      ForbiddenEnvironments = StringList.ToRows(data.forbiddenEnvironments),
      Locations = ParseNear(data.requiredLocation, nameof(data.requiredLocation), file, true),
      ForbiddenLocations = ParseNear(data.forbiddenLocation, nameof(data.forbiddenLocation), file, true),
      Objects = ParseNear(data.requiredObject, nameof(data.requiredObject), file, false),
      ForbiddenObjects = ParseNear(data.forbiddenObject, nameof(data.forbiddenObject), file, false),
    };
  }

  private static List<NearGroup[]>? ParseNear(StringList? list, string field, string file, bool locations) =>
    DistanceRules.Parse(list, field, file)?.Select(row => row.Select(group => new NearGroup(group, locations)).ToArray()).ToList();

  public bool KeysPass()
  {
    var zs = ZoneSystem.instance;
    if (StringList.Forbids(ForbiddenKeys, zs.GetGlobalKey)) return false;
    return Keys == null || StringList.Pass(Keys, zs.GetGlobalKey);
  }

  public bool EnvironmentsPass()
  {
    var env = EnvMan.instance;
    if (StringList.Forbids(ForbiddenEnvironments, env.IsEnvironment)) return false;
    return Environments == null || StringList.Pass(Environments, env.IsEnvironment);
  }

  public static bool UsesNumericKey(Requirements requirements) =>
    (requirements.Keys?.SelectMany(row => row) ?? []).Concat(requirements.ForbiddenKeys?.SelectMany(row => row) ?? []).Any(key =>
    {
      var split = key.Split(' ');
      return split.Length > 1 && int.TryParse(split[1], out _);
    });

  // Instead of adding branches, the vanilla field reads are replaced with values that make the vanilla check fail when needed.
  internal static IEnumerable<CodeInstruction> TranspileKeys(IEnumerable<CodeInstruction> instructions) =>
    ReplaceFieldReads(instructions, nameof(SpawnSystem.SpawnData.m_requiredGlobalKey), nameof(RequiredKey));

  internal static IEnumerable<CodeInstruction> TranspileEnvironments(IEnumerable<CodeInstruction> instructions) =>
    ReplaceFieldReads(instructions, nameof(SpawnSystem.SpawnData.m_requiredEnvironments), nameof(RequiredEnvironments));

  private static IEnumerable<CodeInstruction> ReplaceFieldReads(IEnumerable<CodeInstruction> instructions, string field, string replacement)
  {
    var fieldInfo = AccessTools.Field(typeof(SpawnSystem.SpawnData), field);
    var method = AccessTools.Method(typeof(Requirements), replacement);
    foreach (var instruction in instructions)
    {
      if (instruction.LoadsField(fieldInfo))
      {
        instruction.opcode = OpCodes.Call;
        instruction.operand = method;
      }
      yield return instruction;
    }
  }

  private static string RequiredKey(SpawnSystem.SpawnData spawn) =>
    Loader.Requirements.TryGetValue(spawn, out var requirements) && requirements.HasKeys && !requirements.KeysPass() ? Never : spawn.m_requiredGlobalKey;

  private static List<string> RequiredEnvironments(SpawnSystem.SpawnData spawn) =>
    Loader.Requirements.TryGetValue(spawn, out var requirements) && requirements.HasEnvironments && !requirements.EnvironmentsPass() ? NeverList : spawn.m_requiredEnvironments;

  internal static void CheckLocations(SpawnSystem.SpawnData spawn, Vector3 spawnPoint, ref bool __result)
  {
    if (!__result || !Loader.Requirements.TryGetValue(spawn, out var requirements) || !requirements.HasLocations) return;
    __result = NearbyPass(spawnPoint, requirements.Locations, requirements.ForbiddenLocations);
  }

  internal static void CheckObjects(SpawnSystem.SpawnData spawn, Vector3 spawnPoint, ref bool __result)
  {
    if (!__result || !Loader.Requirements.TryGetValue(spawn, out var requirements) || !requirements.HasObjects) return;
    __result = NearbyPass(spawnPoint, requirements.Objects, requirements.ForbiddenObjects);
  }

  private static bool NearbyPass(Vector3 point, List<NearGroup[]>? required, List<NearGroup[]>? forbidden)
  {
    if (forbidden != null && RowsMatch(forbidden, point)) return false;
    return required == null || RowsMatch(required, point);
  }

  private static bool RowsMatch(List<NearGroup[]> rows, Vector3 point)
  {
    foreach (var row in rows)
    {
      var found = false;
      foreach (var group in row)
      {
        if (!Exists(group, point)) continue;
        found = true;
        break;
      }
      if (!found) return false;
    }
    return true;
  }

  private static bool Exists(NearGroup group, Vector3 point)
  {
    var hashes = group.Resolve();
    if (hashes.Count == 0) return false;
    var zman = ZDOMan.instance;
    var zone = ZoneSystem.GetZone(point);
    // The point can be anywhere inside its zone, so one extra zone is needed.
    var range = (int)(group.Distance / 64f) + 1;
    var sqrDistance = group.Distance * group.Distance;
    Visited.Clear();
    for (var i = -range; i <= range; i++)
    {
      for (var j = -range; j <= range; j++)
      {
        Temp.Clear();
        zman.FindObjects(new Vector2s(zone.x + i, zone.y + j), Temp, Visited);
        foreach (var zdo in Temp)
        {
          var prefab = zdo.GetPrefab();
          if (group.Locations ? !IsLocation(zdo, hashes, prefab) : !hashes.Contains(prefab)) continue;
          var dx = zdo.m_position.x - point.x;
          var dz = zdo.m_position.z - point.z;
          if (dx * dx + dz * dz <= sqrDistance) return true;
        }
      }
    }
    return false;
  }

  private static bool IsLocation(ZDO zdo, HashSet<int> hashes, int prefab) =>
    prefab == ProxyHash && (hashes.Contains(zdo.GetInt(ZDOVars.s_location, 0)) || hashes.Contains(zdo.GetInt(FixGhostInit.ReferenceHash, 0)));
}
