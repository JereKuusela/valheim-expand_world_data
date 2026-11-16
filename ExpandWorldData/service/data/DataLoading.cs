using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Service;

namespace Data;

public class DataLoading
{
  private static readonly string GamePath = Path.GetFullPath(Path.Combine("BepInEx", "config", "data"));
  private static readonly string ProfilePath = Path.GetFullPath(Path.Combine(Paths.ConfigPath, "data"));

  // Each file can have multiple data entries so we need to load them all.
  // Hash is used as key because base64 encoded strings can be loaded too.
  public static Dictionary<int, DataEntry> Data = [];

  public static DataEntry? Get(string name, string fileName)
  {
    var hash = name.GetStableHashCode();
    if (!Data.ContainsKey(hash))
    {
      if (TryParseShorthand(name, out var shorthand))
      {
        Data[hash] = shorthand;
        return shorthand;
      }
      try
      {
        Data[hash] = new DataEntry(new ZPackage(name));
      }
      catch
      {
        if (name.Contains("=") || name.Length > 32)
          Log.Warning($"{fileName}: Can't load data value: {name}");
        else
          Log.Warning($"{fileName}: Can't find data entry: {name}");
        return null;
      }
    }
    return Data[hash];
  }
  private static bool TryParseShorthand(string value, out DataEntry entry)
  {
    entry = null!;
    if (!value.Contains(',')) return false;
    var tkv = value.Split([','], 3).Select(s => s.Trim()).ToArray();
    if (tkv.Length != 3 || !DataEntry.SupportedTypes.Contains(tkv[0].ToLowerInvariant())) return false;
    try
    {
      entry = new DataEntry(tkv);
      return true;
    }
    catch
    {
      return false;
    }
  }
  public static DataEntry? Get(int hash) => Data.ContainsKey(hash) ? Data[hash] : null;

  public static void LoadEntries()
  {
    var prev = Data;
    Data = [];
    ValueGroups.Clear();
    var files = GetSourceFiles(GamePath, ProfilePath, Yaml.BaseDirectory, Pattern);
    var data = Yaml.Read<DataYaml>(files);
    foreach (var d in data)
      ValueGroups.Add(d);
    if (ValueGroups.Count > 0)
      Log.Info($"Loaded {ValueGroups.Count} value groups.");

    // Entries need fully resolved value groups, so two passes are needed.
    ValueGroups.Resolve();
    foreach (var d in data)
      LoadEntry(d, prev);
    PrefabHelper.ClearCache();
    Log.Info($"Loaded {Data.Count} data entries.");
  }

  private static List<string> GetSourceFiles(string gamePath, string profilePath, string baseDirectory, string pattern) =>
    new[] { gamePath, profilePath }.Distinct()
      .SelectMany(path => GetFilesOrEmpty(path, "*.yaml"))
      .Concat(GetFilesOrEmpty(baseDirectory, pattern))
      .Select(Path.GetFullPath).Distinct().ToList();

  private static string[] GetFilesOrEmpty(string path, string pattern)
  {
    try
    {
      return Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
    }
    catch (Exception ex)
    {
      Log.Warning($"Unable to read data directory '{path}': {ex.GetType().Name}: {ex.Message}");
      return [];
    }
  }

  private static void LoadEntry(DataYaml data, Dictionary<int, DataEntry> oldData)
  {
    if (data.name != null)
    {
      var hash = data.name.GetStableHashCode();
      if (Data.ContainsKey(hash))
        Log.Warning($"Duplicate data entry: {data.name}");
      Data[hash] = oldData.TryGetValue(hash, out var prev) ? prev.Reset(data) : new DataEntry(data);
    }
  }

  public static string Pattern = "expand_data*.yaml";
  public static void EnsureDirectories()
  {
    foreach (var path in new[] { GamePath, ProfilePath, Yaml.BaseDirectory }.Distinct())
      EnsureDirectory(path);
  }

  private static void EnsureDirectory(string path)
  {
    try
    {
      Directory.CreateDirectory(path);
    }
    catch (Exception ex)
    {
      // The read attempt reports a warning if this source remains inaccessible.
      Log.Debug($"Unable to create data directory '{path}': {ex.GetType().Name}: {ex.Message}");
    }
  }

  public static void SetupWatcher()
  {
    TryWatch(GamePath, () => Yaml.SetupWatcher(GamePath, "*", LoadEntries));
    if (GamePath != ProfilePath)
      TryWatch(ProfilePath, () => Yaml.SetupWatcher(ProfilePath, "*", LoadEntries));
    TryWatch(Yaml.BaseDirectory, () => Yaml.SetupWatcher(Pattern, LoadEntries));
  }

  private static void TryWatch(string path, Action setup)
  {
    try
    {
      setup();
    }
    catch (Exception ex)
    {
      Log.Warning($"Unable to watch data directory '{path}': {ex.GetType().Name}: {ex.Message}");
    }
  }

}
