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
  public static DataEntry? Get(int hash) => Data.ContainsKey(hash) ? Data[hash] : null;

  public static void LoadEntries()
  {
    var prev = Data;
    Data = [];
    ValueGroups.Clear();
    var files = Directory.GetFiles(GamePath, "*.yaml", SearchOption.AllDirectories)
      .Concat(Directory.GetFiles(ProfilePath, "*.yaml", SearchOption.AllDirectories))
      .Concat(Directory.GetFiles(Yaml.BaseDirectory, Pattern, SearchOption.AllDirectories))
      .Select(Path.GetFullPath).Distinct().ToList();
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
