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
  public static readonly Dictionary<int, List<string>> ValueGroups = [];

  public static DataEntry Get(string name)
  {
    var hash = name.GetStableHashCode();
    if (!Data.ContainsKey(hash))
    {
      try
      {
        Data[hash] = new DataEntry(name);
      }
      catch (Exception e)
      {
        if (name.Contains("=") || name.Length > 32)
          throw new InvalidOperationException($"Can't load data value: {name}", e);
        else
          throw new InvalidOperationException($"Can't find data entry: {name}", e);
      }
    }
    return Data[hash];
  }
  public static DataEntry? Get(int hash) => Data.ContainsKey(hash) ? Data[hash] : null;

  public static void LoadEntries()
  {
    var prev = Data;
    Data = [];
    ValueGroups.Clear();
    var files = Directory.GetFiles(GamePath, "*.yaml")
      .Concat(Directory.GetFiles(ProfilePath, "*.yaml"))
      .Concat(Directory.GetFiles(Yaml.BaseDirectory, Pattern))
      .Select(Path.GetFullPath).Distinct().ToArray();
    foreach (var file in files)
      LoadValues(file, prev);
    if (ValueGroups.Count > 0)
      Log.Info($"Loaded {ValueGroups.Count} value groups.");

    LoadDefaultValueGroups();
    foreach (var kvp in ValueGroups)
      ResolveValues(kvp.Value);
    foreach (var kvp in DefaultValueGroups)
    {
      if (!ValueGroups.ContainsKey(kvp.Key))
        ValueGroups[kvp.Key] = kvp.Value;
    }
    // Entries need fully resolved value groups, so two passes are needed.
    foreach (var file in files)
      LoadEntry(file, prev);
    Log.Info($"Loaded {Data.Count} data entries.");
  }
  private static void LoadValues(string file, Dictionary<int, DataEntry> oldData)
  {
    var yaml = Yaml.LoadList<DataData>(file);
    foreach (var data in yaml)
    {
      if (data.value != null)
      {
        var kvp = Parse.Kvp(data.value);
        var hash = kvp.Key.ToLowerInvariant().GetStableHashCode();
        if (ValueGroups.ContainsKey(hash))
          Log.Warning($"Duplicate value group entry: {kvp.Key} at {file}");
        if (!ValueGroups.ContainsKey(hash))
          ValueGroups[hash] = [];
        ValueGroups[hash].Add(kvp.Value);
      }
      if (data.valueGroup != null && data.values != null)
      {
        var hash = data.valueGroup.ToLowerInvariant().GetStableHashCode();
        if (ValueGroups.ContainsKey(hash))
          Log.Warning($"Duplicate value group entry: {data.valueGroup} at {file}");
        if (!ValueGroups.ContainsKey(hash))
          ValueGroups[hash] = [];
        foreach (var value in data.values)
          ValueGroups[hash].Add(value);
      }
    }
  }
  private static void LoadEntry(string file, Dictionary<int, DataEntry> oldData)
  {
    var yaml = Yaml.LoadList<DataData>(file);
    foreach (var data in yaml)
    {
      if (data.name != null)
      {
        var hash = data.name.GetStableHashCode();
        if (Data.ContainsKey(hash))
          Log.Warning($"Duplicate data entry: {data.name} at {file}");
        Data[hash] = oldData.TryGetValue(hash, out var prev) ? prev.Reset(data) : new DataEntry(data);
      }
    }
  }
  private static readonly Dictionary<int, List<string>> DefaultValueGroups = [];
  private static readonly int WearNTearHash = "wearntear".GetStableHashCode();
  private static readonly int HumanoidHash = "humanoid".GetStableHashCode();
  private static readonly int CreatureHash = "creature".GetStableHashCode();
  private static readonly int StructureHash = "structure".GetStableHashCode();
  private static void LoadDefaultValueGroups()
  {
    if (DefaultValueGroups.Count == 0)
    {
      foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
      {
        if (!prefab) continue;
        prefab.GetComponentsInChildren(ZNetView.m_tempComponents);
        foreach (var component in ZNetView.m_tempComponents)
        {
          var hash = component.GetType().Name.ToLowerInvariant().GetStableHashCode();
          if (!DefaultValueGroups.ContainsKey(hash))
            DefaultValueGroups[hash] = [];
          DefaultValueGroups[hash].Add(prefab.name);
        }
      }
      // Some key codes are hardcoded for legacy reasons.
      DefaultValueGroups[CreatureHash] = DefaultValueGroups[HumanoidHash];
      DefaultValueGroups[StructureHash] = DefaultValueGroups[WearNTearHash];
    }
  }
  private static void ResolveValues(List<string> values)
  {
    for (var i = 0; i < values.Count; ++i)
    {
      var value = values[i];
      if (!value.StartsWith("<", StringComparison.OrdinalIgnoreCase) || !value.EndsWith(">", StringComparison.OrdinalIgnoreCase))
        continue;
      var sub = value.Substring(1, value.Length - 2);
      if (ValueGroups.TryGetValue(sub.ToLowerInvariant().GetStableHashCode(), out var group))
      {
        values.RemoveAt(i);
        values.InsertRange(i, group);
        // Recheck inserted values because value groups can be nested.
        i -= 1;
      }
      else if (DefaultValueGroups.TryGetValue(sub.ToLowerInvariant().GetStableHashCode(), out var group2))
      {
        values.RemoveAt(i);
        values.InsertRange(i, group2);
        // No need to recheck because default value groups are not nested.
        i += group2.Count - 1;
      }
    }
  }

  public static string Pattern = "expand_data*.yaml";
  public static void SetupWatcher()
  {
    if (!Directory.Exists(GamePath))
      Directory.CreateDirectory(GamePath);
    if (!Directory.Exists(ProfilePath))
      Directory.CreateDirectory(ProfilePath);
    if (!Directory.Exists(Yaml.BaseDirectory))
      Directory.CreateDirectory(Yaml.BaseDirectory);
    Yaml.SetupWatcher(GamePath, "*", LoadEntries);
    if (GamePath != ProfilePath)
      Yaml.SetupWatcher(ProfilePath, "*", LoadEntries);
    Yaml.SetupWatcher(Pattern, LoadEntries);
  }

}
