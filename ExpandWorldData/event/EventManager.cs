using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;

namespace ExpandWorld.Event;

public class Manager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_events.yaml");
  public const string Pattern = "expand_events*.yaml";
  public static List<RandomEvent> Originals = [];
  public static bool LoadDelayed;

  public static void CreateConfig()
  {
    if (!Configuration.DataEvents || Helper.IsClient() || File.Exists(FilePath)) return;
    File.WriteAllText(FilePath, Yaml.Serializer().Serialize(RandEventSystem.instance.m_events.Select(Loader.ToData).ToList()));
  }

  public static void ReadConfig()
  {
    if (!Configuration.DataEvents || Helper.IsClient()) return;
    Set(DataManager.Read<Data, RandomEvent>(Pattern, Loader.FromData));
    Configuration.valueEventData.Value = Yaml.Serializer().Serialize(RandEventSystem.instance.m_events.Select(Loader.ToData).ToList());
  }

  public static void FromSetting(string yaml)
  {
    if (!Configuration.DataEvents || LoadDelayed || !Helper.IsClient()) return;
    Set(yaml);
  }

  private static void Set(string yaml)
  {
    if (RandEventSystem.instance == null) return;
    if (Originals.Count == 0) Originals = [.. RandEventSystem.instance.m_events];
    Loader.ExtraData.Clear();
    if (!Configuration.DataEvents || string.IsNullOrEmpty(yaml)) { Restore(); return; }
    try
    {
      var data = Yaml.Deserialize<Data>(yaml, "Events").Select(entry => Loader.FromData(entry, "Events")).ToList();
      if (data.Count == 0) { Log.Warning("Failed to load any event data."); return; }
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data)) return;
      Log.Info($"Reloading event data ({data.Count} entries).");
      RandEventSystem.instance.m_events = data;
    }
    catch (Exception e) { Log.Error(e.Message); Log.Error(e.StackTrace); }
  }

  private static bool AddMissingEntries(List<RandomEvent> entries)
  {
    var missingKeys = Originals.Select(entry => entry.m_name).Distinct().ToHashSet();
    foreach (var entry in entries) missingKeys.Remove(entry.m_name);
    if (missingKeys.Count == 0 || !File.Exists(FilePath)) return false;
    var missing = Originals.Where(entry => missingKeys.Contains(entry.m_name)).ToList();
    Log.Warning($"Adding {missing.Count} missing events to the expand_events.yaml file.");
    File.AppendAllText(FilePath, "\n" + Yaml.Serializer().Serialize(missing.Select(Loader.ToData)));
    return true;
  }

  public static void Toggle()
  {
    EventTiming.Setup(RandEventSystem.instance);
    if (Configuration.DataEvents) { if (Helper.IsServer()) ReadConfig(); else FromSetting(Configuration.valueEventData.Value); }
    else Restore();
  }

  private static void Restore()
  {
    Loader.ExtraData.Clear();
    if (RandEventSystem.instance != null && Originals.Count > 0) RandEventSystem.instance.m_events = [.. Originals];
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfig);
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
public class DelayEventContentLoad { static void Prefix() { if (Configuration.DataEvents) Manager.LoadDelayed = true; } }

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.Last)]
public class InitializeEventContent
{
  static void Postfix()
  {
    if (!Configuration.DataEvents || !Helper.IsServer()) return;
    Manager.CreateConfig();
    Manager.ReadConfig();
  }
}

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
public class InitializeClientEventContent
{
  static void Postfix()
  {
    if (!Configuration.DataEvents || !Manager.LoadDelayed) return;
    Manager.LoadDelayed = false;
    Manager.FromSetting(Configuration.valueEventData.Value);
  }
}

[HarmonyPatch(typeof(RandEventSystem), nameof(RandEventSystem.Awake))]
public class EventTiming
{
  private static float OriginalChance;
  private static float OriginalInterval;
  private static bool Initialized;

  public static void Setup(RandEventSystem system)
  {
    if (!system) return;
    if (!Initialized) { OriginalChance = system.m_eventChance; OriginalInterval = system.m_eventIntervalMin; Initialized = true; }
    system.m_eventChance = Configuration.DataEvents ? Configuration.EventChance : OriginalChance;
    system.m_eventIntervalMin = Configuration.DataEvents ? Configuration.EventInterval : OriginalInterval;
  }

  static void Postfix(RandEventSystem __instance) => Setup(__instance);
}