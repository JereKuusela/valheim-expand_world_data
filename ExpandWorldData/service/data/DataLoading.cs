using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Service;
namespace Data;

public class DataLoading
{
  private static readonly string GamePath = Path.Combine("BepInEx", "config", "data");
  private static readonly string ProfilePath = Path.Combine(Paths.ConfigPath, "config", "data");

  // Each file can have multiple data entries so we need to load them all.
  public static readonly Dictionary<int, DataEntry> Data = [];
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
  public static bool TryGetValueFromGroup(string group, out string value)
  {
    var hash = group.GetStableHashCode();
    if (!ValueGroups.ContainsKey(hash))
    {
      value = group;
      return false;
    }
    var roll = UnityEngine.Random.Range(0, ValueGroups[hash].Count);
    value = ValueGroups[hash][roll];
    return true;
  }
  public static void LoadEntries()
  {
    Data.Clear();
    ValueGroups.Clear();
    var files = Directory.GetFiles(GamePath, "*.yaml")
      .Concat(Directory.GetFiles(ProfilePath, "*.yaml"))
      .Concat(Directory.GetFiles(Yaml.Directory, Pattern)).ToArray();
    foreach (var file in files)
      LoadEntry(file);
    Log.Info($"Loaded {Data.Count} data entries.");
    if (ValueGroups.Count > 0)
      Log.Info($"Loaded {ValueGroups.Count} value groups.");

  }
  private static void LoadEntry(string file)
  {
    var yaml = Yaml.LoadList<DataData>(file);
    foreach (var data in yaml)
    {
      if (data.value != null)
      {
        var kvp = Parse.Kvp(data.value);
        var hash = kvp.Key.GetStableHashCode();
        if (ValueGroups.ContainsKey(hash))
          Log.Warning($"Duplicate value group entry: {kvp.Key} at {file}");
        if (!ValueGroups.ContainsKey(hash))
          ValueGroups[hash] = [];
        ValueGroups[hash].Add(kvp.Value);
      }
      if (data.valueGroup != null && data.values != null)
      {
        var hash = data.valueGroup.GetStableHashCode();
        if (ValueGroups.ContainsKey(hash))
          Log.Warning($"Duplicate value group entry: {data.valueGroup} at {file}");
        if (!ValueGroups.ContainsKey(hash))
          ValueGroups[hash] = [];
        foreach (var value in data.values)
          ValueGroups[hash].Add(value);
      }
      if (data.name != null)
      {
        var hash = data.name.GetStableHashCode();
        if (Data.ContainsKey(hash))
          Log.Warning($"Duplicate data entry: {data.name} at {file}");
        Data[hash] = new DataEntry(data);
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
    Yaml.SetupWatcher(GamePath, "*", LoadEntries);
    if (GamePath != ProfilePath)
      Yaml.SetupWatcher(ProfilePath, "*", LoadEntries);
    Yaml.SetupWatcher(Pattern, LoadEntries);
  }

}
