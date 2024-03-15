
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldData;


[HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
public class StatusManager
{
  private static float DamageTimer = 0f;
  private static readonly float TickRate = 1f;

  private static string PreviousWeather = "";
  private static bool PreviousDay = false;
  private static Heightmap.Biome PreviousBiome = Heightmap.Biome.None;

  static void Postfix(Player __instance, float dt)
  {
    if (__instance != Player.m_localPlayer) return;
    DamageTimer += dt;
    var weather = EnvMan.instance.GetCurrentEnvironment()?.m_name ?? "";
    var day = EnvMan.instance.IsDay();
    var biome = EnvMan.instance.GetBiome();

    RemoveBiomeEffects(__instance, day, biome);
    RemoveWeatherEffects(__instance, day, weather);
    ApplyBiomeEffects(__instance, day, biome);
    ApplyWeatherEffects(__instance, day, weather);

    if (DamageTimer >= TickRate) DamageTimer = 0f;
    PreviousWeather = weather;
    PreviousDay = day;
    PreviousBiome = biome;
  }

  private static void RemoveBiomeEffects(Player player, bool day, Heightmap.Biome biome)
  {
    if (!BiomeManager.TryGetData(PreviousBiome, out var data)) return;
    if (biome != PreviousBiome)
    {
      Remove(player, data.statusEffects);
    }
    if (day != PreviousDay)
    {
      if (day) Remove(player, data.statusEffects.Where(s => !s.day).ToList());
      else Remove(player, data.statusEffects.Where(s => !s.night).ToList());
    }
  }
  private static void RemoveWeatherEffects(Player player, bool day, string weather)
  {
    if (!EnvironmentManager.Extra.TryGetValue(PreviousWeather, out var data)) return;
    if (weather != PreviousWeather)
    {
      Remove(player, data.statusEffects);
    }
    if (day != PreviousDay)
    {
      if (day) Remove(player, data.statusEffects.Where(s => !s.day).ToList());
      else Remove(player, data.statusEffects.Where(s => !s.night).ToList());
    }
  }
  private static void ApplyBiomeEffects(Player player, bool day, Heightmap.Biome biome)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return;
    if (day) Add(player, data.statusEffects.Where(s => s.day).ToList());
    else Add(player, data.statusEffects.Where(s => s.night).ToList());
  }
  private static void ApplyWeatherEffects(Player player, bool day, string weather)
  {
    if (!EnvironmentManager.Extra.TryGetValue(weather, out var data)) return;
    if (day) Add(player, data.statusEffects.Where(s => s.day).ToList());
    else Add(player, data.statusEffects.Where(s => s.night).ToList());
  }

  private static void Remove(Player player, List<Status> es)
  {
    foreach (var statusEffect in es)
      Remove(player, statusEffect);
  }
  private static void Remove(Player player, Status es)
  {
    var seman = player.GetSEMan();
    var statusEffect = seman.GetStatusEffect(es.hash);
    if (statusEffect == null) return;
    // Expiring status effects should expire their own.
    // But permanent ones should be removed.
    if (statusEffect.m_ttl > 0f) return;
    //Log.Info($"Removing {statusEffect.name}");
    seman.RemoveStatusEffect(es.hash);
  }
  private static void Add(Player player, List<Status> es)
  {
    foreach (var statusEffect in es)
      Add(player, statusEffect);
  }

  private static void Add(Player player, Status es)
  {
    if (es.requiredGlobalKeys.Any(k => !ZoneSystem.instance.GetGlobalKey(k))) return;
    if (es.forbiddenGlobalKeys.Any(ZoneSystem.instance.GetGlobalKey)) return;
    if (es.requiredPlayerKeys.Any(k => !player.HaveUniqueKey(k))) return;
    if (es.forbiddenPlayerKeys.Any(player.HaveUniqueKey)) return;
    var seman = player.GetSEMan();
    if (es.reset)
    {
      seman.AddStatusEffect(es.hash, es.reset, 0, 0);
      return;
    }
    var se = ObjectDB.instance.GetStatusEffect(es.hash);
    // To avoid spamming damage calculations, only tick once per second.
    var addDamage = DamageTimer >= TickRate;
    if (se is SE_Burning)
    {
      if (!addDamage) return;
      var exists = seman.GetStatusEffect(es.hash) != null;
      // Heuristic to try detect the damage type.
      if (se.NameHash() == Character.s_statusEffectSpirit)
      {
        var damage = CalculateDamage(seman, es, HitData.DamageType.Spirit);
        if (damage == 0) return;

        // Fire stacks, so the damage must match the tick rate.
        if (!exists) damage *= TickRate * se.m_ttl;
        Log.Debug($"Adding {damage} spirit damage to {se.name}");
        seman.AddStatusEffect(es.hash, false, 0, 0);
        var spirit = (SE_Burning)seman.GetStatusEffect(es.hash);
        spirit.AddSpiritDamage(damage);
      }
      else
      {
        var damage = CalculateDamage(seman, es, HitData.DamageType.Fire);
        if (damage == 0) return;
        // Fire stacks, so the damage must match the tick rate.
        if (!exists) damage *= TickRate * se.m_ttl;
        Log.Debug($"Adding {damage} fire damage to {se.name}");
        seman.AddStatusEffect(es.hash, false, 0, 0);
        var burning = (SE_Burning)seman.GetStatusEffect(es.hash);
        burning.AddFireDamage(damage);
      }
    }
    else if (se is SE_Poison)
    {
      if (!addDamage) return;
      var damage = CalculateDamage(seman, es, HitData.DamageType.Poison);
      if (damage == 0) return;
      // Poison doesn't stack so full damage can always be added.
      Log.Debug($"Adding {damage} poison damage to {se.name}");

      seman.AddStatusEffect(es.hash, false, 0, 0);
      var poison = (SE_Poison)seman.GetStatusEffect(es.hash);
      poison.AddDamage(damage);
    }
    else
    {
      seman.AddStatusEffect(es.hash, false, 0, 0);
      var effect = seman.GetStatusEffect(es.hash);
      effect.m_time = effect.m_ttl - es.duration;
      if (effect is SE_Shield shield)
        shield.m_absorbDamage = es.damage;
    }
  }

  private static float CalculateDamage(SEMan seman, Status es, HitData.DamageType damageType)
  {
    var damage = es.damage;
    var damageIgnoreArmor = es.damageIgnoreArmor;
    if (seman.m_character)
    {
      var mod = seman.m_character.GetDamageModifier(damageType);
      var multi = ModToMultiplier(mod);
      if (multi < 1 && es.immuneWithResist)
        multi = 0;
      damage *= multi;
      damageIgnoreArmor *= multi;
      if (damage > 0)
      {
        var armor = seman.m_character.GetBodyArmor();
        damage = ApplyArmor(damage, armor);
      }
    }
    return damage + damageIgnoreArmor + es.damageIgnoreAll;
  }
  private static float ApplyArmor(float dmg, float ac)
  {
    float num = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
    if ((double)ac < (double)dmg / 2.0)
      num = dmg - ac;
    return num;
  }
  private static float ModToMultiplier(HitData.DamageModifier mod)
  {
    if (mod == HitData.DamageModifier.Resistant) return 0.5f;
    if (mod == HitData.DamageModifier.VeryResistant) return 0.25f;
    if (mod == HitData.DamageModifier.Weak) return 1.5f;
    if (mod == HitData.DamageModifier.VeryWeak) return 2f;
    if (mod == HitData.DamageModifier.Immune) return 0f;
    if (mod == HitData.DamageModifier.Ignore) return 0f;
    return 1f;
  }
}

public class StatusData
{
  public string name = "";
  public string requiredGlobalKeys = "";
  public string forbiddenGlobalKeys = "";
  public string requiredPlayerKeys = "";
  public string forbiddenPlayerKeys = "";
  public bool day = false;
  public bool night = false;
  public bool immuneWithResist = false;
  public float duration = 0f;
  public float damage = 0f;
  public float damageIgnoreArmor = 0f;
  public float damageIgnoreAll = 0f;
}

public class Status
{
  public int hash;
  public float duration;
  public float damage;
  public float damageIgnoreAll;
  public float damageIgnoreArmor;
  public bool reset;
  public bool day;
  public bool night;
  public bool immuneWithResist;
  public List<string> requiredGlobalKeys = [];
  public List<string> forbiddenGlobalKeys = [];
  public List<string> requiredPlayerKeys = [];
  public List<string> forbiddenPlayerKeys = [];
  public Status(StatusData status)
  {
    hash = status.name.GetStableHashCode();
    duration = status.duration;
    damage = status.damage;
    damageIgnoreArmor = status.damageIgnoreArmor;
    damageIgnoreAll = status.damageIgnoreAll;
    day = status.day;
    night = status.night;
    // Both are disabled by default which makes no sense.
    // For better use experience, enable both by default.
    if (!day && !night)
    {
      day = true;
      night = true;
    }
    immuneWithResist = status.immuneWithResist;
    requiredGlobalKeys = DataManager.ToList(status.requiredGlobalKeys);
    forbiddenGlobalKeys = DataManager.ToList(status.forbiddenGlobalKeys);
    requiredPlayerKeys = DataManager.ToList(status.requiredPlayerKeys);
    forbiddenPlayerKeys = DataManager.ToList(status.forbiddenPlayerKeys);

    // Custom duration is handled manually.
    // Also damage effects shouldn't be reseted (since it messed up the damage calculation).
    reset = status.duration == 0f && status.damage == 0f && status.damageIgnoreArmor == 0f && status.damageIgnoreAll == 0f;
  }
}