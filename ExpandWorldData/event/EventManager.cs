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
  public static bool HasData { get; private set; }

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml)
  {
    if (!LoadDelayed) Instance.FromSetting(yaml);
  }

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => EventManager.FilePath;
    protected override string Pattern => EventManager.Pattern;
    protected override bool DataEnabled => Configuration.DataEvents;
    protected override string ConfigValue { get => Configuration.valueEventData.Value; set => Configuration.valueEventData.Value = value; }
    protected override bool Set(Dictionary<string, string> files) => EventManager.Set(files);
    protected override void WriteDefaultConfig() => File.WriteAllText(FilePath, Yaml.Serializer().Serialize(RandEventSystem.instance.m_events.Select(Loader.ToData).ToList()));
  }
  private static readonly Sync Instance = new();

  private static bool Set(Dictionary<string, string> files)
  {
    if (RandEventSystem.instance == null) return false;
    if (Originals.Count == 0) Originals = [.. RandEventSystem.instance.m_events];
    if (files.Count == 0)
    {
      HasData = false;
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
      HasData = true;
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
    CreateConfigs();
    ReadConfigs();
  }

  internal static void InitializeClientData()
  {
    if (!LoadDelayed) return;
    LoadDelayed = false;
    FromSetting(Configuration.valueEventData.Value);
  }

  internal static void ApplyTiming(RandEventSystem __instance)
  {
    if (__instance == null) return;
    __instance.m_eventChance = Configuration.EventChance;
    __instance.m_eventIntervalMin = Configuration.EventInterval;
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataEvents, ReadConfigs);
}
