using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Common;

namespace Service;

public class Yaml
{
  public static string BaseDirectory = Path.Combine(Paths.ConfigPath, "expand_world");
  // Dependencies use this field.
  public static string Directory = BaseDirectory;
  public static string BackupDirectory = Path.Combine(Paths.ConfigPath, "expand_world_backups");
  public static bool WorldLoading;
  public static DateTime WorldLoadedAt = DateTime.MinValue;


  public static List<T> Read<T>(string pattern)
  {
    if (!System.IO.Directory.Exists(BaseDirectory))
      System.IO.Directory.CreateDirectory(BaseDirectory);
    var files = System.IO.Directory.GetFiles(BaseDirectory, pattern, SearchOption.AllDirectories).Reverse().ToList();
    return Read<T>(files);
  }

  public static List<T> Read<T>(List<string> files)
  {
    List<T> result = [];
    foreach (var file in files)
    {
      try
      {
        var lines = File.ReadAllText(file);
        result.AddRange(Deserialize<T>(lines, file));
      }
      catch (Exception ex)
      {
        Log.Error($"Error reading {Path.GetFileName(file)}: {ex.Message}");
      }
    }
    return result;
  }

  public static Heightmap.Biome ToBiomes(string biomeStr)
  {
    Heightmap.Biome result = 0;
    if (biomeStr == "")
    {
      foreach (var biome in Enum.GetValues(typeof(Heightmap.Biome)))
        result |= (Heightmap.Biome)biome;
    }
    else
    {
      var biomes = Parse.Split(biomeStr);
      foreach (var biome in biomes)
      {
        if (Enum.TryParse<Heightmap.Biome>(biome, true, out var number))
          result |= number;
        else
        {
          if (int.TryParse(biome, out var value)) result += value;
          else throw new InvalidOperationException($"Invalid biome {biome}.");
        }
      }
    }
    return result;
  }
  public static void SetupWatcher(ConfigFile config)
  {
    FileSystemWatcher watcher = new(Path.GetDirectoryName(config.ConfigFilePath), Path.GetFileName(config.ConfigFilePath));
    watcher.Changed += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.Created += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.Renamed += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  private static void ReadConfigValues(string path, ConfigFile config)
  {
    if (!File.Exists(path)) return;
    BackupFile(path);
    try
    {
      config.Reload();
    }
    catch
    {
      Log.Error($"There was an issue loading your {config.ConfigFilePath}");
      Log.Error("Please check your config entries for spelling and format!");
    }
  }
  public static void SetupWatcher(string pattern, Action<string> action) => SetupWatcher(Paths.ConfigPath, pattern, action);
  public static void SetupWatcher(string folder, string pattern, Action<string> action)
  {
    FileSystemWatcher watcher = new(folder, pattern);
    watcher.Created += (s, e) => action(e.FullPath);
    watcher.Changed += (s, e) => action(e.FullPath);
    watcher.Renamed += (s, e) => action(e.FullPath);
    watcher.Deleted += (s, e) => action(e.FullPath);
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  public static void SetupWatcher(string folder, string pattern, Action action)
  {
    FileSystemWatcher watcher = new(folder, pattern);
    watcher.Created += (s, e) => action();
    watcher.Changed += (s, e) => action();
    watcher.Renamed += (s, e) => action();
    watcher.Deleted += (s, e) => action();
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  public static void SetupWatcher(string pattern, Action action) => SetupWatcher(BaseDirectory, pattern, file =>
  {
    BackupFile(file);
    action();
  });
  // Files written by the mod that are loaded synchronously right after, so the delayed watcher event must be skipped.
  private static readonly Dictionary<string, DateTime> IgnoredWrites = [];
  public static void IgnoreWrites(string pattern)
  {
    foreach (var file in System.IO.Directory.GetFiles(BaseDirectory, pattern, SearchOption.AllDirectories))
      IgnoredWrites[file] = File.GetLastWriteTimeUtc(file);
  }
  public static void SetupDataWatcher(string pattern, ConfigEntry<bool> setting, Action action)
  {
    FileSystemWatcher watcher = new(BaseDirectory, pattern);
    void Changed(string path)
    {
      if (!File.Exists(path))
      {
        // Reading also recreates a deleted main file, even if the data is disabled.
        if (ExpandWorldData.Configuration.DataReload) action();
        return;
      }
      if (IgnoredWrites.TryGetValue(path, out var ignored) && File.GetLastWriteTimeUtc(path) <= ignored) return;
      if (!setting.Value && (WorldLoading || File.GetLastWriteTimeUtc(path) <= WorldLoadedAt)) return;
      BackupFile(path);
      if (!setting.Value)
      {
        Log.Info($"Automatically enabling {setting.Definition.Key} after editing {Path.GetFileName(path)}.");
        setting.Value = true;
      }
      else if (ExpandWorldData.Configuration.DataReload)
        action();
    }
    watcher.Created += (s, e) => Changed(e.FullPath);
    watcher.Changed += (s, e) => Changed(e.FullPath);
    watcher.Renamed += (s, e) => Changed(e.FullPath);
    watcher.Deleted += (s, e) => Changed(e.FullPath);
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  private static void BackupFile(string path)
  {
    if (!File.Exists(path)) return;
    if (!System.IO.Directory.Exists(BackupDirectory))
      System.IO.Directory.CreateDirectory(BackupDirectory);
    var stamp = DateTime.Now.ToString("yyyy-MM-dd");
    var name = $"{Path.GetFileNameWithoutExtension(path)}_{stamp}{Path.GetExtension(path)}.bak";
    File.Copy(path, Path.Combine(BackupDirectory, name), true);
  }

  public static void Init()
  {
    if (!System.IO.Directory.Exists(BaseDirectory))
      System.IO.Directory.CreateDirectory(BaseDirectory);
  }

  public static IDeserializer Deserializer() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
  .WithTypeConverter(new global::Data.DataFieldConverter()).WithTypeConverter(new StringListConverter()).WithYamlFormatter(formatter).Build();
  public static IDeserializer DeserializerUnSafe() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
  .WithTypeConverter(new global::Data.DataFieldConverter()).WithTypeConverter(new StringListConverter()).WithYamlFormatter(formatter).IgnoreUnmatchedProperties().Build();
  public static ISerializer Serializer() => new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).DisableAliases()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).WithTypeConverter(new Data.DataFieldConverter()).WithTypeConverter(new StringListConverter()).WithYamlFormatter(formatter)
      .Build();
  private static readonly YamlFormatter formatter = new() { NumberFormat = NumberFormatInfo.InvariantInfo };

  public static List<T> Deserialize<T>(string raw, string fileName)
  {
    try
    {
      return Deserializer().Deserialize<List<T>>(raw) ?? [];
    }
    catch (Exception ex1)
    {
      Log.Error($"{fileName}: {ex1.Message}");
      try
      {
        return DeserializerUnSafe().Deserialize<List<T>>(raw) ?? [];
      }
      catch (Exception)
      {
        return [];
      }
    }
  }
  public static bool TryDeserialize<T>(string raw, string fileName, out List<T> result)
  {
    try
    {
      result = Deserializer().Deserialize<List<T>>(raw) ?? [];
      return true;
    }
    catch (Exception ex1)
    {
      Log.Error($"{fileName}: {ex1.Message}");
      try
      {
        result = DeserializerUnSafe().Deserialize<List<T>>(raw) ?? [];
        return true;
      }
      catch (Exception ex2)
      {
        Log.Error($"{fileName}: {ex2.Message}");
        result = [];
        return false;
      }
    }
  }
  public static bool TryDeserialize<T>(Dictionary<string, string> files, out List<T> result)
    => TryDeserialize<T, T>(files, (item, fileName) => item, out result);

  public static bool TryDeserialize<T, TEntry>(Dictionary<string, string> files, Func<T, string, TEntry> converter, out List<TEntry> result)
  {
    result = [];
    foreach (var file in files)
    {
      if (!TryDeserialize<T>(file.Value, file.Key, out var parsed))
        return false;
      result.AddRange(parsed.Select(item => converter(item, file.Key)));
    }
    return true;
  }
  public static List<T> LoadList<T>(string file) where T : new()
  {
    if (!File.Exists(file)) return [];
    return Deserialize<T>(File.ReadAllText(file), file);
  }

  ///<summary>Returns true if any matching yaml file contains the given field.</summary>
  public static bool HasField(string pattern, string field)
  {
    if (!System.IO.Directory.Exists(BaseDirectory)) return false;
    return HasField(System.IO.Directory.GetFiles(BaseDirectory, pattern, SearchOption.AllDirectories), field);
  }

  public static bool HasField(IEnumerable<string> files, string field)
  {
    var prefix = $"{field}:";
    return files.Any(file => File.ReadLines(file).Any(line => line.TrimStart().TrimStart('-', ' ').StartsWith(prefix)));
  }

  ///<summary>Removes the given fields (including nested blocks) from list entries in matching yaml files. Returns true if any file was changed.</summary>
  public static bool RemoveFields(string pattern, string[] fields)
  {
    if (!System.IO.Directory.Exists(BaseDirectory)) return false;
    var changedAny = false;
    foreach (var file in System.IO.Directory.GetFiles(BaseDirectory, pattern, SearchOption.AllDirectories))
    {
      var lines = File.ReadAllLines(file).ToList();
      var changed = false;
      for (var i = 0; i < lines.Count; i++)
      {
        var trimmed = lines[i].TrimStart();
        var dashed = trimmed.StartsWith("- ");
        var key = dashed ? trimmed.Substring(2).TrimStart() : trimmed;
        if (!fields.Any(field => key.StartsWith($"{field}:"))) continue;
        var keyIndent = lines[i].Length - key.Length;
        // Nested content is either more indented or a compact sequence at the same indent.
        var last = i;
        for (var j = i + 1; j < lines.Count; j++)
        {
          var lineTrimmed = lines[j].TrimStart();
          if (lineTrimmed == "") continue;
          var lineIndent = lines[j].Length - lineTrimmed.Length;
          if (lineIndent > keyIndent || (lineIndent == keyIndent && lineTrimmed.StartsWith("-"))) last = j;
          else break;
        }
        var dashPrefix = lines[i].Substring(0, keyIndent);
        lines.RemoveRange(i, last - i + 1);
        // Keep the list item marker on the next sibling field when the removed field started the item.
        if (dashed && i < lines.Count)
        {
          var next = lines[i].TrimStart();
          if (lines[i].Length - next.Length == keyIndent && !next.StartsWith("-"))
            lines[i] = dashPrefix + next;
        }
        changed = true;
        i--;
      }
      if (!changed) continue;
      ExpandWorldData.Migrations.BackupFile(file);
      File.WriteAllLines(file, lines);
      changedAny = true;
    }
    return changedAny;
  }

  ///<summary>Inserts a missing field into existing list entries in matching yaml files, preserving comments/formatting. Returns true if any file was changed.</summary>
  public static bool InsertMissingField(string pattern, string keyField, string targetField, Dictionary<string, string> valuesByKey)
  {
    if (!System.IO.Directory.Exists(BaseDirectory)) return false;
    return InsertMissingField(System.IO.Directory.GetFiles(BaseDirectory, pattern, SearchOption.AllDirectories), keyField, targetField, valuesByKey);
  }

  public static bool InsertMissingField(IEnumerable<string> files, string keyField, string targetField, Dictionary<string, string> valuesByKey)
  {
    var changedAny = false;
    var targetPrefix = $"{targetField}:";
    foreach (var file in files)
    {
      var lines = File.ReadAllLines(file).ToList();
      var changed = false;
      for (var i = 0; i < lines.Count; i++)
      {
        var trimmed = lines[i].TrimStart();
        if (!trimmed.StartsWith("-")) continue;
        var separator = trimmed.IndexOf(':');
        if (separator < 0 || !trimmed.Substring(1, separator - 1).Trim().Equals(keyField, StringComparison.Ordinal)) continue;
        var indent = lines[i].Length - trimmed.Length;
        var value = trimmed.Substring(separator + 1).Trim();
        var commentIndex = value.IndexOf('#');
        if (commentIndex >= 0) value = value.Substring(0, commentIndex).Trim();
        value = value.Trim('"', '\'');
        if (!valuesByKey.TryGetValue(value, out var newValue) || newValue == "") continue;
        // Find the block extent (until the next top-level list item or EOF) to check for an existing field and insertion point.
        var end = i + 1;
        var hasField = false;
        while (end < lines.Count)
        {
          var lineTrimmed = lines[end].TrimStart();
          var lineIndent = lines[end].Length - lineTrimmed.Length;
          if (lineTrimmed.StartsWith("- ") && lineIndent <= indent) break;
          if (lineTrimmed.StartsWith(targetPrefix)) { hasField = true; break; }
          end++;
        }
        if (hasField) continue;
        var prefix = new string(' ', indent + 2);
        if (newValue.Contains("\n") || newValue.StartsWith("- "))
        {
          var rows = newValue.Replace("\r\n", "\n").Split('\n');
          lines.InsertRange(i + 1, new[] { $"{prefix}{targetField}:" }.Concat(rows.Select(row => $"{prefix}  {row}")));
        }
        else lines.Insert(i + 1, $"{prefix}{targetField}: {newValue}");
        changed = true;
      }
      if (!changed) continue;
      ExpandWorldData.Migrations.BackupFile(file);
      File.WriteAllLines(file, lines);
      changedAny = true;
    }
    return changedAny;
  }
}
