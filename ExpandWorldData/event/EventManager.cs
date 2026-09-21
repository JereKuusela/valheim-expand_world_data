using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
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
    if (RandEventSystem.instance == null) { ExpandWorldData.Patcher.Update(EWD.Harmony); return; }
    if (Originals.Count == 0) Originals = [.. RandEventSystem.instance.m_events];
    if (string.IsNullOrEmpty(yaml))
    {
      Log.Warning("Failed to load any event data. No changes done.");
      return;
    }
    RemoveSpawnMetadata(RandEventSystem.instance.m_events);
    Loader.ExtraData.Clear();
    try
    {
      var data = Yaml.Deserialize<Data>(yaml, "Events").Select(entry => Loader.FromData(entry, "Events")).ToList();
      if (data.Count == 0) { Log.Warning("Failed to load any event data."); return; }
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data)) return;
      Log.Info($"Reloading event data ({data.Count} entries).");
      RandEventSystem.instance.m_events = data;
    }
    catch (Exception e) { Log.Error(e.Message); Log.Error(e.StackTrace); }
    finally { ExpandWorldData.Patcher.Update(EWD.Harmony); }
  }

  private static void RemoveSpawnMetadata(IEnumerable<RandomEvent> events)
  {
    foreach (var spawn in events.SelectMany(entry => entry.m_spawn))
    {
      Spawn.Loader.Data.Remove(spawn);
      Spawn.Loader.Objects.Remove(spawn);
    }
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
    if (Configuration.DataEvents)
    {
      ExpandWorldData.Patcher.Update(EWD.Harmony);
      ApplyTiming(RandEventSystem.instance);
      if (Helper.IsServer())
      {
        CreateConfig();
        ReadConfig();
      }
      else FromSetting(Configuration.valueEventData.Value);
    }
    else
    {
      Log.Warning("Disabling event data requires a restart to restore the original event data and timing.");
      ExpandWorldData.Patcher.Update(EWD.Harmony);
    }
  }

  internal static void DelayClientLoad() => LoadDelayed = true;

  internal static void InitializeServerData()
  {
    if (!Helper.IsServer()) return;
    CreateConfig();
    ReadConfig();
  }

  internal static void InitializeClientData()
  {
    if (!LoadDelayed) return;
    LoadDelayed = false;
    FromSetting(Configuration.valueEventData.Value);
  }

  internal static void ApplyTiming(RandEventSystem system)
  {
    if (!system) return;
    system.m_eventChance = Configuration.EventChance;
    system.m_eventIntervalMin = Configuration.EventInterval;
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfig);
}
