using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;
using YamlDotNet.Serialization.NamingConventions;

namespace Service;

public class Yaml
{
  public static string BaseDirectory = Path.Combine(Paths.ConfigPath, "expand_world");
  // Dependencies use this field.
  public static string Directory = BaseDirectory;
  public static string BackupDirectory = Path.Combine(Paths.ConfigPath, "expand_world_backups");


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
  .WithYamlFormatter(formatter).Build();
  public static IDeserializer DeserializerUnSafe() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
  .WithYamlFormatter(formatter).IgnoreUnmatchedProperties().Build();
  public static ISerializer Serializer() => new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).DisableAliases()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).WithYamlFormatter(formatter)
      .WithAttributeOverride<Color>(c => c.gamma, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.grayscale, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.linear, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.maxColorComponent, new YamlIgnoreAttribute())
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
  public static List<T> LoadList<T>(string file) where T : new()
  {
    if (!File.Exists(file)) return [];
    return Deserialize<T>(File.ReadAllText(file), file);
  }
}
