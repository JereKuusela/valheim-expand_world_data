using System.Collections.Generic;
using System.IO;

namespace ExpandWorldData;

/// <summary>Common create/read/sync/cleanup lifecycle shared by YAML-backed data managers.</summary>
public abstract class SyncedDataManager
{
  // Tracks every instance so pending sync data can be applied only after all managers are initialized.
  private static readonly List<SyncedDataManager> Instances = [];
  protected SyncedDataManager() { Instances.Add(this); }

  // Shared across all instances: they're always initialized together in the same call, never individually.
  private static bool Initialized;
  protected bool Pending;

  protected abstract string FilePath { get; }
  protected abstract string Pattern { get; }
  protected abstract bool DataEnabled { get; }
  protected abstract string ConfigValue { get; set; }
  /// <summary>False for managers whose default file is optional (e.g. only written when extra entries exist).</summary>
  protected virtual bool RequireFileExistsCheckOnRead => true;

  protected abstract bool Set(Dictionary<string, string> files);
  /// <summary>Writes the default config file content. May no-op if there's nothing to write.</summary>
  protected abstract void WriteDefaultConfig();
  protected virtual void AfterCreateConfigs() { }

  public void CreateConfigs()
  {
    if (Helper.IsClient() || !DataEnabled || File.Exists(FilePath)) return;
    WriteDefaultConfig();
    AfterCreateConfigs();
  }

  public void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (DataEnabled)
    {
      if (RequireFileExistsCheckOnRead && !File.Exists(FilePath))
      {
        // Watcher will trigger reload.
        CreateConfigs();
        return;
      }
      var files = DataManager.Read(Pattern);
      if (files == null || !Apply(files)) return;
      ConfigValue = string.Join("\n", files.Values);
    }
    else if (Apply([]))
    {
      ConfigValue = "";
    }
  }

  public void FromSetting(string yaml)
  {
    if (!Helper.IsClient()) return;
    if (!Initialized) { Pending = true; return; }
    Apply(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }

  private bool Apply(Dictionary<string, string> files)
  {
    if (!Set(files)) return false;
    Refresh.Patches();
    return true;
  }

  private void ApplyPending()
  {
    if (!Pending) return;
    Pending = false;
    FromSetting(ConfigValue);
  }

  /// <summary>Replays queued sync updates for every manager, once all of them are initialized.</summary>
  public static void PostInitialize()
  {
    Initialized = true;
    foreach (var instance in Instances) instance.ApplyPending();
  }

  /// <summary>Resets every manager's sync state. Order between managers doesn't matter.</summary>
  public static void CleanUpAll()
  {
    Initialized = false;
    foreach (var instance in Instances) instance.Pending = false;
  }
}
