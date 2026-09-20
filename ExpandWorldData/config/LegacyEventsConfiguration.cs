using System;
using System.Globalization;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using Service;

namespace ExpandWorldData;

public static class LegacyEventsConfiguration
{
  private const string Section = "1. General";
  private static readonly string FilePath = Path.Combine(Paths.ConfigPath, "ExpandWorldEvents.cfg");

  public static void Migrate(ConfigFile target)
  {
    if (!File.Exists(FilePath)) return;
    try
    {
      var source = new ConfigFile(FilePath, false);
      var multipleEvents = source.Bind(Section, "Multiple events", false).Value;
      var checkPerPlayer = source.Bind(Section, "Check per player", false).Value;
      var minimumDistance = source.Bind(Section, "Minimum distance between events", 100f).Value;
      var eventChance = source.Bind(Section, "Random event chance", 20f).Value;
      var eventInterval = source.Bind(Section, "Random event interval", 46f).Value;

      Configuration.configMultipleEvents.Value = multipleEvents;
      Configuration.configCheckPerPlayer.Value = checkPerPlayer;
      Configuration.configEventMinimumDistance.Value = minimumDistance.ToString(CultureInfo.InvariantCulture);
      Configuration.configEventChance.Value = eventChance.ToString(CultureInfo.InvariantCulture);
      Configuration.configEventInterval.Value = eventInterval.ToString(CultureInfo.InvariantCulture);
      target.Save();
      File.Delete(FilePath);
      Log.Info("Migrated Expand World Events configuration to Expand World Data.");
    }
    catch (Exception e)
    {
      Log.Warning($"Failed to migrate {FilePath}. The legacy configuration was kept. {e.Message}");
    }
  }
}