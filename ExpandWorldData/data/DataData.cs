using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldData;

public class DataData
{
  public string name = "";
  [DefaultValue("")]
  public string connection = "";
  [DefaultValue(null)]
  public string[]? ints = null;
  [DefaultValue(null)]
  public string[]? floats = null;
  [DefaultValue(null)]
  public string[]? strings = null;
  [DefaultValue(null)]
  public string[]? longs = null;
  [DefaultValue(null)]
  public string[]? vecs = null;
  [DefaultValue(null)]
  public string[]? quats = null;
  [DefaultValue(null)]
  public string[]? bytes = null;
}

public class DefaultData
{
  private static string[]? knownKeys;
  private static string[] KnownKeys => knownKeys ??= GenerateKeys();
  private static string[] GenerateKeys()
  {
    List<string> keys = [.. StaticKeys];
    List<Assembly> assemblies = [Assembly.GetAssembly(typeof(ZNetView)), .. Chainloader.PluginInfos.Values.Where(p => p.Instance != null).Select(p => p.Instance.GetType().Assembly)];
    var baseType = typeof(MonoBehaviour);
    var types = assemblies.SelectMany(s =>
    {
      try
      {
        return s.GetTypes();
      }
      catch (ReflectionTypeLoadException e)
      {
        return e.Types.Where(t => t != null);
      }
    }).Where(t =>
    {
      try
      {
        return baseType.IsAssignableFrom(t);
      }
      catch
      {
        return false;
      }
    }).ToArray();
    keys.AddRange(types.Select(t => $"HasFields{t.Name}"));
    keys.AddRange(types.SelectMany(t => t.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(f => $"{t.Name}.{f.Name}")));
    keys.AddRange(typeof(ZDOVars).GetFields(BindingFlags.Static | BindingFlags.Public).Select(f => f.Name.Replace("s_", "")));
    keys.AddRange(typeof(ZDOVars).GetFields(BindingFlags.Static | BindingFlags.Public).Select(f => FirstLetterUpper(f.Name.Replace("s_", ""))));
    for (var i = 0; i < 10; i++)
    {
      keys.Add($"item{i}");
      keys.Add($"quality{i}");
      keys.Add($"variant{i}");
    }
    keys.AddRange(AnimationKeys.Select(k => $"${k}"));
    return [.. keys.Distinct()];
  }
  private static string FirstLetterUpper(string s) => char.ToUpper(s[0]) + s.Substring(1);
  private static readonly string[] StaticKeys = [
    "alive_time",
    "attachJoint",
    "body_avel",
    "body_vel",
    "emote_oneshot",
    "HaveSaddle",
    "haveTarget",
    "IsBlocking",
    "max_health",
    "picked_time",
    "relPos",
    "relRot",
    "scale",
    "scaleScalar",
    "vel",
    "lastWorldTime",
    "spawntime",
    "spawnpoint",
    "HasFields",
    "user_u",
    "user_i",
    "RodOwner_u",
    "RodOwner_i",
    "CatchID_u",
    "CatchID_i",
    "target_u",
    "target_i",
    "spawn_id_u",
    "spawn_id_i",
    "parent_id_u",
    "parent_id_i",
    // CLLC mod
    "CL&LC effect",
    // Structure / Spawner Tweaks
    "override_amount",
    "override_attacks",
    "override_biome",
    "override_boss",
    "override_collision",
    "override_compendium",
    "override_component",
    "override_conversion",
    "override_cover_offset",
    "override_data",
    "override_delay",
    "override_destroy",
    "override_discover",
    "override_dungeon_enter_hover",
    "override_dungeon_enter_text",
    "override_dungeon_exit_hover",
    "override_dungeon_exit_text",
    "override_dungeon_weather",
    "override_effect",
    "override_event",
    "override_faction",
    "override_fall",
    "override_fuel",
    "override_fuel_effect",
    "override_globalkey",
    "override_growth",
    "override_health",
    "override_input_effect",
    "override_interact",
    "override_item",
    "override_item_offset",
    "override_item_stand_prefix",
    "override_item_stand_range",
    "override_items",
    "override_level_chance",
    "override_maximum_amount",
    "override_maximum_cover",
    "override_maximum_fuel",
    "override_maximum_level",
    "override_max_near",
    "override_max_total",
    "override_minimum_amount",
    "override_minimum_level",
    "override_name",
    "override_near_radius",
    "override_output_effect",
    "override_pickable_spawn",
    "override_pickable_respawn",
    "override_render",
    "override_resistances",
    "override_respawn",
    "override_restrict",
    "override_smoke",
    "override_spawn",
    "override_spawn_condition",
    "override_spawn_effect",
    "override_spawn_max_y",
    "override_spawn_offset",
    "override_spawn_radius",
    "override_spawnarea_spawn",
    "override_spawnarea_respawn",
    "override_spawn_item",
    "override_start_effect",
    "override_text",
    "override_text_biome",
    "override_text_check",
    "override_text_extract",
    "override_text_happy",
    "override_text_sleep",
    "override_text_space",
    "override_topic",
    "override_trigger_distance",
    "override_trigger_noise",
    "override_unlock",
    "override_use_effect",
    "override_water",
    "override_wear",
    "override_weather",
    // Marketplace
    "KGmarketNPC",
    "KGnpcProfile",
    "KGnpcModelOverride",
    "KGnpcNameOverride",
    "KGnpcDialogue",
    "KGleftItem",
    "KGrightItem",
    "KGhelmetItem",
    "KGchestItem",
    "KGlegsItem",
    "KGcapeItem",
    "KGhairItem",
    "KGhairItemColor",
    "KGLeftItemBack",
    "KGRightItemBack",
    "KGinteractAnimation",
    "KGgreetingAnimation",
    "KGbyeAnimation",
    "KGgreetingText",
    "KGbyeText",
    "KGskinColor",
    "KGcraftingAnimation",
    "KGbeardItem",
    "KGbeardColor",
    "KGinteractSound",
    "KGtextSize",
    "KGtextHeight",
    "KGperiodicAnimation",
    "KGperiodicAnimationTime",
    "KGperiodicSound",
    "KGperiodicSoundTime",
    "KGnpcScale"];

  private static readonly string[] AnimationKeys = [
    "alert",
    "footstep",
    "forward_speed",
    "sideway_speed",
    "anim_speed",
    "statef",
    "statei",
    "blocking",
    "attack",
    "flapping",
    "falling",
    "onGround",
    "intro",
    "crouching",
    "encumbered",
    "equipping",
    "attach_bed",
    "attach_chair",
    "attach_throne",
    "attach_sitship",
    "attach_mast",
    "attach_dragon",
    "attach_lox",
    "bow_aim",
    "reload_crossbow",
    "crafting",
    "visible",
    "turn_speed",
    "idle",
    "flying",
    "body_forward_speed",
    "inWater",
    "onGround",
    "minoraction",
    "minoraction_fast",
    "emote"
  ];

  private static Dictionary<int, string>? hashToKey;
  private static Dictionary<int, string> HashToKey => hashToKey ??= KnownKeys.ToDictionary(Hash, x => x);
  public static string Convert(int hash) => HashToKey.TryGetValue(hash, out var key) ? key : hash.ToString();
  public static int Hash(string key)
  {
    if (key.StartsWith("$", StringComparison.InvariantCultureIgnoreCase))
    {
      var hash = ZSyncAnimation.GetHash(key.Substring(1));
      if (key == "$anim_speed") return hash;
      return 438569 + hash;
    }
    return key.GetStableHashCode();
  }
  public static bool Exists(int hash) => hashToKey == null || HashToKey.ContainsKey(hash);

  public static DataData[] Data = [
    new()
    {
      name = "infinite_health",
      floats = ["health, 1E30"]
    },
    new()
    {
      name = "default_health",
      floats = ["health, 0"]
    },
    new()
    {
      name = "st_healthy",
      floats = ["health, 1E30"],
      ints = ["override_wear, 0"]
    },
    new()
    {
      name = "st_damaged",
      floats = ["health, 1E30"],
      ints = ["override_wear, 1"]
    },
    new()
    {
      name = "st_broken",
      floats = ["health, 1E30"],
      ints = ["override_wear, 3"]
    }
  ];
}

/*
[HarmonyPatch(typeof(ZDO))]
public class KeyCollector
{

  [HarmonyPatch(nameof(ZDO.Set), typeof(int), typeof(string)), HarmonyPrefix]
  static void String(int hash)
  {
    if (DefaultData.Exists(hash)) return;
    var stack = new System.Diagnostics.StackTrace();
    EWD.Log.LogWarning($"Found new string key: {hash}\n{stack}");
  }
  [HarmonyPatch(nameof(ZDO.Set), typeof(int), typeof(int)), HarmonyPrefix]
  static void Int(int hash)
  {
    if (DefaultData.Exists(hash)) return;
    var stack = new System.Diagnostics.StackTrace();
    EWD.Log.LogWarning($"Found new int key: {hash}\n{stack}");
  }
  [HarmonyPatch(nameof(ZDO.Set), typeof(int), typeof(long)), HarmonyPrefix]
  static void Long(int hash)
  {
    if (DefaultData.Exists(hash)) return;
    var stack = new System.Diagnostics.StackTrace();
    EWD.Log.LogWarning($"Found new long key: {hash}\n{stack}");
  }
  [HarmonyPatch(nameof(ZDO.Set), typeof(int), typeof(float)), HarmonyPrefix]
  static void Float(int hash)
  {
    if (DefaultData.Exists(hash)) return;
    var stack = new System.Diagnostics.StackTrace();
    EWD.Log.LogWarning($"Found new float key: {hash}\n{stack}");
  }
  [HarmonyPatch(nameof(ZDO.Set), typeof(int), typeof(Vector3)), HarmonyPrefix]
  static void Vector3(int hash)
  {
    if (DefaultData.Exists(hash)) return;
    var stack = new System.Diagnostics.StackTrace();
    EWD.Log.LogWarning($"Found new Vector3 key: {hash}\n{stack}");
  }
}
[HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.GetHash))]
public class ZSyncAnimation_GetHash_Patch
{
  static void Postfix(string name, int __result)
  {
    if (DefaultData.Exists(__result + 438569)) return;
    EWD.Log.LogWarning($"Found new animation key: {name}");
  }
}
*/