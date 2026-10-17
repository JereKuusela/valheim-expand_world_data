using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Service;

namespace ExpandWorldData;

public enum SetResult
{
  Applied,
  /// <summary>Previous state is kept (parse error, nothing usable).</summary>
  Failed,
  /// <summary>Files were rewritten on disk, so they must be read again.</summary>
  Rewritten
}

/// <summary>Create/read lifecycle shared by YAML-backed data managers. Missing files are always created, applied only when enabled.</summary>
public abstract class YamlDataManager
{
  protected abstract string FilePath { get; }
  protected abstract string Pattern { get; }
  protected abstract bool DataEnabled { get; }
  protected static SetResult Result(bool applied) => applied ? SetResult.Applied : SetResult.Failed;
  /// <summary>Applies the files. Empty means default data. Must not change the state unless Applied is returned. Exceptions are logged and count as Failed.</summary>
  protected abstract SetResult Set(Dictionary<string, string> files);
  /// <summary>Writes the default config file content. May no-op if there's nothing to write.</summary>
  protected abstract void WriteDefaultConfig();
  /// <summary>Migrations rewrite files, so only the server with the setting enabled may run them.</summary>
  public static bool MigrationEnabled => Configuration.DataMigration && Helper.IsServer();
  /// <summary>Upgrades yaml files on disk before they are read. Only called when migrations are enabled. Returns true if anything was written.</summary>
  protected virtual bool Migrate() => false;
  /// <summary>Runs after files were applied (or defaults restored when disabled).</summary>
  protected virtual void OnApplied(Dictionary<string, string> files) { }
  // Disabled data must not touch anything unless custom data was previously applied.
  private bool Customized;

  public void CreateConfigs()
  {
    if (Helper.IsClient() || File.Exists(FilePath)) return;
    WriteDefaultConfig();
    Yaml.IgnoreWrites(Pattern);
  }

  public void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    // Written even when disabled so users have a file to start from.
    CreateConfigs();
    if (!DataEnabled)
    {
      if (Apply([]) == SetResult.Applied) OnApplied([]);
      return;
    }
    if (MigrationEnabled && Migrate()) Yaml.IgnoreWrites(Pattern);
    // Rewrites are read right away so the watcher doesn't cause a second load.
    List<Dictionary<string, string>> previousReads = [];
    for (var attempt = 0; attempt < 16; attempt++)
    {
      var files = DataManager.Read(Pattern);
      if (files == null) return;
      if (previousReads.Any(previous => previous.Count == files.Count && files.All(file => previous.TryGetValue(file.Key, out var value) && value == file.Value)))
      {
        Log.Warning($"{Pattern}: Migration repeated previously read contents. Skipped.");
        return;
      }
      previousReads.Add(files);
      var result = Apply(files);
      if (result == SetResult.Applied)
      {
        OnApplied(files);
        return;
      }
      if (result == SetResult.Failed) return;
      Yaml.IgnoreWrites(Pattern);
    }
    Log.Warning($"{Pattern}: Files keep changing while loading. Skipped.");
  }

  protected SetResult Apply(Dictionary<string, string> files)
  {
    if (files.Count == 0 && !Customized) return SetResult.Applied;
    SetResult result;
    try
    {
      result = Set(files);
    }
    catch (System.Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return SetResult.Failed;
    }
    if (result != SetResult.Applied) return result;
    Customized = files.Count > 0;
    Refresh.Request(Regen.Patches);
    return result;
  }
}
