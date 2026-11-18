using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using NUnit.Framework;

namespace ExpandWorldData.Tests;

[TestFixture, NonParallelizable]
public class ServerOnlyTests
{
  [TestCase("DataBiome", false)]
  [TestCase("DataWorld", false)]
  [TestCase("DataTerritory", false)]
  [TestCase("DataEnvironments", false)]
  [TestCase("DataClutter", false)]
  [TestCase("DataAltBiomes", false)]
  [TestCase("DataSpawns", false)]
  [TestCase("DataDrops", false)]
  [TestCase("DataEvents", true)]
  [TestCase("DataPersistentEvents", false)]
  [TestCase("LegacyGeneration", false)]
  [TestCase("CustomWaterColor", false)]
  [TestCase("ScaleLocationAltitudeRequirement", false)]
  [TestCase("MultipleEvents", true)]
  [TestCase("CheckPerPlayer", true)]
  [TestCase("DataLocation", true)]
  [TestCase("DataVegetation", true)]
  [TestCase("DataDungeons", true)]
  [TestCase("DataRooms", true)]
  [TestCase("ZoneSpawners", true)]
  [TestCase("RandomLocations", true)]
  [TestCase("DataReload", true)]
  [TestCase("DataMigration", true)]
  public void EffectiveSettingsPreserveSavedChoices(string name, bool supported)
  {
    var type = typeof(Configuration);
    var mode = type.GetProperty(nameof(Configuration.ServerOnly))!;
    var field = type.GetField("config" + name)!;
    var getter = type.GetProperty(name)!;
    var previousMode = mode.GetValue(null);
    var previousEntry = field.GetValue(null);
    var config = new ConfigFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cfg"), false);
    var entry = config.Bind("test", name, true);
    try
    {
      field.SetValue(null, entry);
      mode.SetValue(null, true);
      Assert.That(getter.GetValue(null), Is.EqualTo(supported));
      Assert.That(entry.Value, Is.True, "Saved choice must not be overwritten.");
      mode.SetValue(null, false);
      Assert.That(getter.GetValue(null), Is.True, "Normal mode must restore the saved choice.");
      entry.Value = false;
      Assert.That(getter.GetValue(null), Is.False);
      if (name.StartsWith("Data") && name != "DataReload" && name != "DataMigration")
      {
        Assert.That(Configuration.IsDataAllowed(entry), Is.True);
        mode.SetValue(null, true);
        Assert.That(Configuration.IsDataAllowed(entry), Is.EqualTo(supported));
        Assert.That(entry.Value, Is.False, "An ignored file must not enable its saved setting.");
      }
    }
    finally
    {
      field.SetValue(null, previousEntry);
      mode.SetValue(null, previousMode);
    }
  }
}
