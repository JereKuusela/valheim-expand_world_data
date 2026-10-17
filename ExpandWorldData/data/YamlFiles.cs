using System;
using System.Collections.Generic;
using System.IO;
using Service;

namespace ExpandWorldData;

/// <summary>File writing shared by the yaml data managers.</summary>
public static class YamlFiles
{
  /// <summary>Writes items to expand_{name}[mod].yaml files. Defaults skip existing files, otherwise (log) items are appended.</summary>
  public static void SavePerMod<T>(string name, IEnumerable<T> items, Func<T, string> prefab, bool log)
  {
    Dictionary<string, List<T>> perFile = [];
    foreach (var item in items)
    {
      var mod = AssetTracker.GetModFromPrefab(prefab(item));
      var suffix = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(suffix))
        perFile[suffix] = [];
      perFile[suffix].Add(item);
      if (log)
        Log.Warning($"{mod}: {prefab(item)}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_{name}{kvp.Key}.yaml");
      // Defaults are only written to missing files, otherwise they would be duplicated.
      if (!log && File.Exists(file)) continue;
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value);
      File.WriteAllText(file, yaml);
    }
  }
}
