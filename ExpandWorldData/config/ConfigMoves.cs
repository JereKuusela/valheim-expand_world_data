using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace ExpandWorldData;

///<summary>Carries over values of settings that were moved to another section. Orphaned entries are the file values that weren't bound yet.</summary>
public static class ConfigMoves
{
  public const string OldSection = "2. Features";
  public const string EventSection = "5. Events";
  public static readonly string[] EventSettings =
  [
    "Multiple events",
    "Check per player",
    "Minimum distance between events",
    "Random event chance",
    "Random event interval",
  ];

  // Call before the settings are bound.
  public static Dictionary<string, string> Read(ConfigFile config)
  {
    Dictionary<string, string> values = [];
    var orphans = Orphans(config);
    foreach (var name in EventSettings)
    {
      if (orphans.ContainsKey(new ConfigDefinition(EventSection, name))) continue;
      if (orphans.TryGetValue(new ConfigDefinition(OldSection, name), out var value))
        values[name] = value;
    }
    return values;
  }

  // Protected in BepInEx.
  private static Dictionary<ConfigDefinition, string> Orphans(ConfigFile config) =>
    (Dictionary<ConfigDefinition, string>)typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(config);

  // Call after the settings are bound.
  public static void Apply(ConfigFile config, Dictionary<string, string> values)
  {
    if (values.Count == 0) return;
    foreach (var pair in values)
      config[EventSection, pair.Key].SetSerializedValue(pair.Value);
    config.Save();
  }
}
