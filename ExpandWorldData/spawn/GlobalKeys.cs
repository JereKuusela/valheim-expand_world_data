using System;
using System.Linq;

namespace ExpandWorld.Spawn;

public class GlobalKeys
{
  internal static void Mutate(ZoneSystem __instance, ref string name)
  {
    var key = ZoneSystem.GetKeyValue(name.ToLower(), out var value, out _);
    if (value.StartsWith("--", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var decrease))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue - decrease}" : $"{key} -{decrease}";
    else if (value.StartsWith("++", StringComparison.OrdinalIgnoreCase) && int.TryParse(value.Substring(2), out var increase))
      name = __instance.GetGlobalKey(key, out var previous) && int.TryParse(previous, out var previousValue) ? $"{key} {previousValue + increase}" : $"{key} {increase}";
  }
  internal static bool CheckRequirement(ZoneSystem __instance, string name, ref bool __result)
  {
    var split = name.Trim().Split(' ');
    if (split.Length < 2 || !int.TryParse(split[1], out var requiredValue)) return true;
    __result = __instance.m_globalKeysValues.TryGetValue(split[0].ToLower(), out var rawValue) && int.TryParse(rawValue, out var value) && value >= requiredValue;
    return false;
  }
  internal static void Consume(SpawnSystem.SpawnData critter)
  {
    if (!string.IsNullOrEmpty(critter.m_requiredGlobalKey))
      ConsumeKey(critter.m_requiredGlobalKey);
    if (Loader.Requirements.TryGetValue(critter, out var requirements) && requirements.Keys != null)
    {
      // Pick the keys first, so that rows sharing a key don't see each other's consumption.
      var picked = requirements.Keys.Select(row => row.FirstOrDefault(key => IsNumeric(key) && ZoneSystem.instance.GetGlobalKey(key))).ToList();
      foreach (var key in picked)
        if (key != null) ConsumeKey(key);
    }
  }

  private static bool IsNumeric(string key)
  {
    var split = key.Trim().Split(' ');
    return split.Length > 1 && int.TryParse(split[1], out _);
  }

  private static void ConsumeKey(string key)
  {
    var split = key.Trim().Split(' ');
    if (split.Length > 1 && int.TryParse(split[1], out var amount)) ZoneSystem.instance.SetGlobalKey($"{split[0]} --{amount}");
  }
}
