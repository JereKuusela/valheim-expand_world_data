using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx.Configuration;
using ExpandWorldData.Locations;
using NUnit.Framework;

namespace ExpandWorldData.Tests;

[TestFixture, NonParallelizable]
public class ServerOnlyTests
{

  private static readonly MethodInfo GetIcons = typeof(ZoneSystemPatches).GetMethod(
    "GetLocationIcons", BindingFlags.Static | BindingFlags.NonPublic)!;
  private static readonly FieldInfo ExtraInfoField = typeof(LocationExtra).GetField("ExtraInfo")!;

  [TestCase(false, "StartTemple", "BogWitch_Camp", "StartTemple", true)]
  [TestCase(true, "StartTemple", "BogWitch_Camp", "BogWitch_Camp", false)]
  [TestCase(true, "StartTemple", "DeathMark, 0.3", "DeathMark, 0.3", false)]
  [TestCase(false, "Ping,0.24", "QuestionMark, 0.3", "Ping,0.24", true)]
  [TestCase(true, "StartTemple", "StartTemple,2,pulse", "StartTemple,2,pulse", false)]
  [TestCase(true, "StartTemple", "false", "StartTemple", true)]
  [TestCase(false, "false", "BogWitch_Camp", null, false)]
  [TestCase(true, "false", "false", null, false)]
  [TestCase(false, "true", "false", "CustomLocation", true)]
  [TestCase(true, "false", "true", "CustomLocation", false)]
  public void GetLocationIconsPreservesConfiguredTokens(bool placed, string always, string placedIcon, string? expected, bool offset)
  {
    WithIsolatedLocationData(info =>
    {
      var location = CreateLocation("CustomLocation");
      var extra = (LocationExtraInfo)FormatterServices.GetUninitializedObject(typeof(LocationExtraInfo));
      extra.Data = new LocationYaml { iconAlways = always, iconPlaced = placedIcon };
      info.Add(location, extra);

      var icons = RunPrefix(location, placed, out var originalPosition);
      AssertIcon(icons, originalPosition, expected, offset);
    });
  }

  [TestCase(false, true, false, "CustomLocation")]
  [TestCase(true, false, true, "CustomLocation")]
  [TestCase(false, false, true, null)]
  [TestCase(true, false, false, null)]
  public void GetLocationIconsKeepsVanillaFallbackWithoutExtraData(bool placed, bool always, bool placedIcon, string? expected)
  {
    WithIsolatedLocationData(_ =>
    {
      var location = CreateLocation("CustomLocation");
      var type = location.GetType();
      type.GetField("m_iconAlways")!.SetValue(location, always);
      type.GetField("m_iconPlaced")!.SetValue(location, placedIcon);

      var icons = RunPrefix(location, placed, out var originalPosition);
      AssertIcon(icons, originalPosition, expected, false);
    });
  }

  private static void WithIsolatedLocationData(Action<IDictionary> assertIcons)
  {
    var previous = ExtraInfoField.GetValue(null);
    var isolated = (IDictionary)Activator.CreateInstance(ExtraInfoField.FieldType)!;
    try
    {
      ExtraInfoField.SetValue(null, isolated);
      assertIcons(isolated);
    }
    finally
    {
      ExtraInfoField.SetValue(null, previous);
    }
  }

  private static object CreateLocation(string name)
  {
    var zoneSystemType = GetIcons.GetParameters()[0].ParameterType;
    var locationType = zoneSystemType.GetNestedType("ZoneLocation", BindingFlags.Public)!;
    var location = Activator.CreateInstance(locationType)!;
    // Only the serialized name is needed; no Unity GameObject or asset loading.
    var prefabField = locationType.GetField("m_prefab")!;
    var prefab = FormatterServices.GetUninitializedObject(prefabField.FieldType);
    prefabField.FieldType.GetField("m_name")!.SetValue(prefab, name);
    prefabField.SetValue(location, prefab);
    return location;
  }

  private static IDictionary RunPrefix(object location, bool placed, out object originalPosition)
  {
    // ZoneSystem is a MonoBehaviour; construct only its data, without starting Unity.
    var zoneSystemType = GetIcons.GetParameters()[0].ParameterType;
    var system = FormatterServices.GetUninitializedObject(zoneSystemType);
    var instancesField = zoneSystemType.GetField("m_locationInstances")!;
    var instances = (IDictionary)Activator.CreateInstance(instancesField.FieldType)!;
    var types = instancesField.FieldType.GetGenericArguments();
    var instanceType = types[1];
    var instance = Activator.CreateInstance(instanceType)!;
    instanceType.GetField("m_location")!.SetValue(instance, location);
    var positionField = instanceType.GetField("m_position")!;
    originalPosition = Activator.CreateInstance(positionField.FieldType, 64f, 2f, 128f)!;
    positionField.SetValue(instance, originalPosition);
    instanceType.GetField("m_placed")!.SetValue(instance, placed);
    instances.Add(Activator.CreateInstance(types[0])!, instance);
    instancesField.SetValue(system, instances);

    var icons = (IDictionary)Activator.CreateInstance(GetIcons.GetParameters()[1].ParameterType)!;
    Assert.That(GetIcons.Invoke(null, new object[] { system, icons }), Is.EqualTo(false));
    return icons;
  }

  private static void AssertIcon(IDictionary icons, object originalPosition, string? expected, bool offset)
  {
    Assert.That(icons.Count, Is.EqualTo(expected == null ? 0 : 1));
    if (expected == null) return;

    var enumerator = icons.GetEnumerator();
    Assert.That(enumerator.MoveNext(), Is.True);
    var icon = enumerator.Entry;
    Assert.That(icon.Value, Is.EqualTo(expected));
    var positionType = originalPosition.GetType();
    foreach (var axis in new[] { "x", "z" })
      Assert.That(positionType.GetField(axis)!.GetValue(icon.Key),
        Is.EqualTo(positionType.GetField(axis)!.GetValue(originalPosition)));
    var y = (float)positionType.GetField("y")!.GetValue(icon.Key)!;
    Assert.That(y, Is.EqualTo(offset ? 2f + 0.00001f : 2f).Within(0.000001f));
  }

  [TestCase("", "NewLocation", true)]
  [TestCase("false", "NewLocation", true)]
  [TestCase("StartTemple", "NewLocation", true)]
  [TestCase("true", "StartTemple", true)]
  [TestCase("true", "NewLocation", false)]
  [TestCase("Hammer", "StartTemple", false)]
  [TestCase("UnknownLocation", "StartTemple", false)]
  [TestCase("StartTemple,2", "NewLocation", false)]
  [TestCase("StartTemple,2,pulse", "NewLocation", false)]
  [TestCase("starttemple", "NewLocation", false)]
  public void OriginalIconNameHintsAreForWarningsOnly(string icon, string location, bool supported)
  {
    var knownIcons = new HashSet<string>(StringComparer.Ordinal) { "StartTemple", "Trader" };
    var method = typeof(LocationManager).GetMethod("IsVanillaLocationIcon", BindingFlags.NonPublic | BindingFlags.Static)!;
    var result = (bool)method.Invoke(null, new object[] { icon, location, knownIcons })!;
    Assert.That(result, Is.EqualTo(supported));
  }

  [TestCase("DeathMark, 0.3", "CustomLocation", "DeathMark, 0.3")]
  [TestCase("Hammer", "StartTemple", "Hammer")]
  [TestCase("UnknownLocation", "StartTemple", "UnknownLocation")]
  [TestCase("StartTemple,2,pulse", "StartTemple", "StartTemple,2,pulse")]
  [TestCase("StartTemple", "CustomLocation:Main", "StartTemple")]
  [TestCase("Hildir_camp", "StartTemple", "Hildir_camp")]
  [TestCase("BogWitch_Camp", "CustomLocation:Main", "BogWitch_Camp")]
  [TestCase("true", "StartTemple", "StartTemple")]
  [TestCase("false", "StartTemple", "")]
  [TestCase("", "StartTemple", "")]
  public void ServerOnlyPreservesIconNamesForLocationLookups(string icon, string prefab, string expected)
  {
    var method = typeof(LocationManager).GetMethod("ResolveLocationIcon", BindingFlags.NonPublic | BindingFlags.Static)!;
    var value = (string)method.Invoke(null, new object[] { icon, prefab })!;
    Assert.That(value, Is.EqualTo(expected));
  }

  [TestCase("DeathMark, 0.3", "CustomLocation")]
  [TestCase("Hildir_camp", "StartTemple")]
  public void DiagnosticsDoNotChangeIconTokens(string icon, string prefab)
  {
    var method = typeof(LocationManager).GetMethod("ResolveLocationIcon", BindingFlags.NonPublic | BindingFlags.Static)!;
    var value = (string)method.Invoke(null, new object[] { icon, prefab })!;
    Assert.That(value, Is.EqualTo(icon));
  }

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
