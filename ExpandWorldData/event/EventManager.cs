using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using Service;

namespace ExpandWorld.Event;

public class EventManager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_events.yaml");
  public const string Pattern = "expand_events*.yaml";
  public static List<RandomEvent> Originals = [];
  public static bool LoadDelayed;

  public static void CreateConfigs()
  {
    if (!Configuration.DataEvents || Helper.IsClient() || File.Exists(FilePath)) return;
    File.WriteAllText(FilePath, Yaml.Serializer().Serialize(RandEventSystem.instance.m_events.Select(Loader.ToData).ToList()));
  }

  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (!Configuration.DataEvents)
    {
      if (Set([]))
        Configuration.valueEventData.Value = "";
      return;
    }
    var files = DataManager.Read(Pattern);
    if (files == null || !Set(files)) return;
    Configuration.valueEventData.Value = string.Join("\n", files.Values);
  }

  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient() && !LoadDelayed) Set(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }

  private static bool Set(Dictionary<string, string> files)
  {
    if (RandEventSystem.instance == null) return false;
    if (Originals.Count == 0) Originals = [.. RandEventSystem.instance.m_events];
    if (files.Count == 0)
    {
      Patcher.SetEnabled(false);
      EWD.Instance.InvokeRegenerate();
      return true;
    }
    try
    {
      List<RandomEvent> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<Data>(file.Value, file.Key, out var parsed)) return false;
        data.AddRange(parsed.Select(entry => Loader.FromData(entry, file.Key)));
      }
      if (data.Count == 0) return false;
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data)) return false;
      Log.Info($"Reloading event data ({data.Count} entries).");
      RemoveSpawnMetadata(RandEventSystem.instance.m_events);
      Loader.ExtraData.Clear();
      RandEventSystem.instance.m_events = data;
      Patcher.SetEnabled(true);
      EWD.Instance.InvokeRegenerate();
      return true;
    }
    catch (Exception e) { Log.Error(e.Message); Log.Error(e.StackTrace); return false; }
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

  internal static void DelayClientLoad() => LoadDelayed = true;

  internal static void InitializeServerData()
  {
    if (!Helper.IsServer()) return;
    CreateConfigs();
    ReadConfigs();
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

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataEvents, ReadConfigs);
}
