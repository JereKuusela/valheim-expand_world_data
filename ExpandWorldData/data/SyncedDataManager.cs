using System.Collections.Generic;
using Service;

namespace ExpandWorldData;

/// <summary>Adds client sync of the yaml contents to the shared create/read lifecycle.</summary>
public abstract class SyncedDataManager : YamlDataManager
{
  // Tracks every instance so pending sync data can be applied only after all managers are initialized.
  private static readonly List<SyncedDataManager> Instances = [];
  protected SyncedDataManager() { Instances.Add(this); }

  // Shared across all instances: they're always initialized together in the same call, never individually.
  private static bool Initialized;
  protected bool Pending;

  protected abstract string ConfigValue { get; set; }
  /// <summary>Builds the value sent to clients. Override to strip server-only or redundant data.</summary>
  protected virtual string ToSyncValue(Dictionary<string, string> files) => string.Join("\n", files.Values);

  protected override void OnApplied(Dictionary<string, string> files)
  {
    ConfigValue = files.Count == 0 ? "" : ToSyncValue(files);
  }

  public void FromSetting(string yaml)
  {
    if (!Helper.IsClient()) return;
    if (!Initialized) { Pending = true; return; }
    Apply(string.IsNullOrEmpty(yaml) ? [] : new() { ["synchronized"] = yaml });
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
