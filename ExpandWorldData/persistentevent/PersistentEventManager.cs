using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Data;
using Service;

namespace ExpandWorldData.PersistentEvents;

public class PersistentEventManager
{
  public static readonly string FilePath = Path.Combine(Yaml.Directory, "expand_persistent_events.yaml");
  public const string Pattern = "expand_persistent_events*.yaml";
  public static List<PersistentEventSystem.PersistentEvent> Originals = [];
  public static bool HasData { get; private set; }

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => PersistentEventManager.FilePath;
    protected override string Pattern => PersistentEventManager.Pattern;
    protected override bool DataEnabled => Configuration.DataPersistentEvents;
    protected override string ConfigValue { get => Configuration.valuePersistentEventData.Value; set => Configuration.valuePersistentEventData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => PersistentEventManager.Set(files);
    protected override void WriteDefaultConfig()
    {
      if (Originals.Count == 0) return;
      File.WriteAllText(FilePath, TriggerComment() + Yaml.Serializer().Serialize(Originals.Select(PersistentEventLoader.ToData).ToList()));
    }
  }
  private static readonly Sync Instance = new();

  // Vanilla has no start condition in the event data, events are started by destroying specific prefabs.
  private static string TriggerComment()
  {
    if (!ZNetScene.instance) return "";
    var lines = ZNetScene.instance.m_prefabs
      .Where(prefab => prefab)
      .Select(prefab => (prefab.name, trigger: prefab.GetComponent<TriggerPersistentEventOnDestroy>()))
      .Where(pair => pair.trigger != null)
      .Select(pair => $"# {pair.trigger._eventInternalName}: {(pair.trigger._stopEvent ? "stopped" : "started")} when {pair.name} is destroyed.")
      .OrderBy(line => line)
      .ToList();
    if (lines.Count == 0) return "";
    return "# Vanilla persistent events have no automatic start. Use customChance and customInterval to start them automatically.\n" + string.Join("\n", lines) + "\n";
  }

  public static void Initialize()
  {
    if (PersistentEventSystem.instance == null) return;
    Originals = [.. PersistentEventSystem.instance.m_possibleEvents];
    HasData = false;
  }

  private static SetResult Set(Dictionary<string, string> files)
  {
    var system = PersistentEventSystem.instance;
    if (system == null || Originals.Count == 0) return SetResult.Failed;
    var previousExtra = PersistentEventLoader.Extra;
    var previousObjectExtra = PersistentEventLoader.ObjectExtra;
    PersistentEventLoader.Extra = [];
    PersistentEventLoader.ObjectExtra = [];
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(system, files);
    }
    finally
    {
      if (result != SetResult.Applied)
      {
        PersistentEventLoader.Extra = previousExtra;
        PersistentEventLoader.ObjectExtra = previousObjectExtra;
      }
      else
        PersistentEventRuntime.Reset(system);
    }
  }

  private static SetResult LoadAndApply(PersistentEventSystem system, Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      Log.Info($"Reloading default persistent event data ({Originals.Count} entries).");
      system.m_possibleEvents = [.. Originals];
      PersistentEventLoader.Extra.Clear();
      PersistentEventLoader.ObjectExtra.Clear();
      HasData = false;
      return SetResult.Applied;
    }
    if (!Yaml.TryDeserialize<PersistentEventYaml, PersistentEventSystem.PersistentEvent>(files, PersistentEventLoader.FromData, out var data)) return SetResult.Failed;
    if (data.Count == 0) return SetResult.Failed;
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(data)) return SetResult.Rewritten;
    Log.Info($"Reloading persistent event data ({data.Count} entries).");
    system.m_possibleEvents = data;
    HasData = true;
    return SetResult.Applied;
  }

  private static bool AddMissingEntries(List<PersistentEventSystem.PersistentEvent> entries)
  {
    var missing = YamlFiles.GetMissingEntries(Originals, entries.Select(entry => entry.internalName), entry => entry.internalName);
    if (missing.Count == 0 || !File.Exists(FilePath)) return false;
    Log.Warning($"Adding {missing.Count} missing persistent events to the expand_persistent_events.yaml file.");
    File.AppendAllText(FilePath, "\n" + Yaml.Serializer().Serialize(missing.Select(PersistentEventLoader.ToData)));
    return true;
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataPersistentEvents, ReadConfigs);
}
