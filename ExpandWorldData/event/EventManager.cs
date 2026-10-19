using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Data;
using Service;

namespace ExpandWorldData.Event;

public class EventManager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_events.yaml");
  public const string Pattern = "expand_events*.yaml";
  public static List<RandomEvent> Originals = [];
  public static bool HasData { get; private set; }

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => EventManager.FilePath;
    protected override string Pattern => EventManager.Pattern;
    protected override bool DataEnabled => Configuration.DataEvents;
    protected override string ConfigValue { get => Configuration.valueEventData.Value; set => Configuration.valueEventData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => EventManager.Set(files);
    protected override void WriteDefaultConfig()
    {
      if (Originals.Count == 0) return;
      File.WriteAllText(FilePath, Yaml.Serializer().Serialize(Originals.Select(Loader.ToData).ToList()));
    }
  }
  private static readonly Sync Instance = new();

  public static void Initialize()
  {
    if (RandEventSystem.instance == null) return;
    Originals = [.. RandEventSystem.instance.m_events];
    HasData = false;
  }

  private static SetResult Set(Dictionary<string, string> files)
  {
    var system = RandEventSystem.instance;
    if (system == null) return SetResult.Failed;
    using var metadata = new Spawn.MetadataLoad();
    var previousSpawns = system.m_events.SelectMany(entry => entry.m_spawn);
    var previousExtra = Loader.ExtraData;
    Loader.ExtraData = [];
    var result = SetResult.Failed;
    try
    {
      var loaded = LoadAndApply(files);
      if (loaded == SetResult.Applied)
        metadata.Commit(previousSpawns, system.m_events.SelectMany(entry => entry.m_spawn));
      return result = loaded;
    }
    finally
    {
      if (result != SetResult.Applied) Loader.ExtraData = previousExtra;
    }
  }

  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    if (RandEventSystem.instance == null || Originals.Count == 0) return SetResult.Failed;
    if (files.Count == 0)
    {
      Log.Info($"Reloading default event data ({Originals.Count} entries).");
      Restore();
      return SetResult.Applied;
    }
    if (!Yaml.TryDeserialize<Data, RandomEvent>(files, Loader.FromData, out var data)) return SetResult.Failed;
    if (data.Count == 0) return SetResult.Failed;
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(data)) return SetResult.Rewritten;
    Log.Info($"Reloading event data ({data.Count} entries).");
    RandEventSystem.instance.m_events = data;
    HasData = true;
    return SetResult.Applied;
  }

  private static void Restore()
  {
    Loader.ExtraData.Clear();
    RandEventSystem.instance.m_events = [.. Originals];
    HasData = false;
  }

  private static bool AddMissingEntries(List<RandomEvent> entries)
  {
    var missing = YamlFiles.GetMissingEntries(Originals, entries.Select(entry => entry.m_name), entry => entry.m_name);
    if (missing.Count == 0 || !File.Exists(FilePath)) return false;
    Log.Warning($"Adding {missing.Count} missing events to the expand_events.yaml file.");
    File.AppendAllText(FilePath, "\n" + Yaml.Serializer().Serialize(missing.Select(Loader.ToData)));
    return true;
  }

  internal static void InitializeServerData() => ReadConfigs();

  internal static void ApplyTiming(RandEventSystem __instance)
  {
    if (__instance == null) return;
    __instance.m_eventChance = Configuration.EventChance;
    __instance.m_eventIntervalMin = Configuration.EventInterval;
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataEvents, ReadConfigs);
}
