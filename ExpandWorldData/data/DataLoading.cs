using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class DataLoading
{
  private static readonly string FileName = "expand_data.yaml";
  private static readonly string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  private static readonly string Pattern = "expand_data*.yaml";


  public static void Initialize()
  {
    Load();
  }
  public static void Load()
  {
    if (Helper.IsClient()) return;
    if (!File.Exists(FilePath))
    {
      var yaml = DataManager.Serializer().Serialize(DefaultData.Data);
      File.WriteAllText(FilePath, yaml);
      // Watcher triggers reload.
      return;
    }

    var data = FromFile();
    if (data.Count == 0)
    {
      EWD.Log.LogWarning($"Failed to load any data data.");
      return;
    }
    EWD.Log.LogInfo($"Reloading data ({data.Count} entries).");
    foreach (var item in data)
      ZDOData.Register(item);
  }
  public static void Save(ZDOData data)
  {
    if (Helper.IsClient()) return;
    var yaml = File.Exists(FilePath) ? File.ReadAllText(FilePath) : DataManager.Serializer().Serialize(DefaultData.Data);
    yaml += "\n" + DataManager.Serializer().Serialize(new DataData[] { ToData(data) });
    File.WriteAllText(FilePath, yaml);
  }
  ///<summary>Loads all yaml files returning the deserialized vegetation entries.</summary>
  private static List<ZDOData> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return DataManager.Deserialize<DataData>(yaml, FileName).Select(FromData)
        .Where(data => data.Name != "").ToList();
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
    return new();
  }

  public static ZDOData FromData(DataData data)
  {
    ZDOData zdo = new()
    {
      Name = data.name
    };
    foreach (var value in data.floats ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length < 2) continue;
      var str = string.Join(",", split.Skip(1));
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Floats.Add(hash, Parse.Float(str));
    }
    foreach (var value in data.ints ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length < 2) continue;
      var str = string.Join(",", split.Skip(1));
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Ints.Add(hash, Parse.Int(str));
    }
    foreach (var value in data.longs ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length < 2) continue;
      var str = string.Join(",", split.Skip(1));
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Longs.Add(hash, Parse.Long(str));
    }
    foreach (var value in data.strings ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length < 2) continue;
      var str = string.Join(",", split.Skip(1));
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Strings.Add(hash, str);
    }
    foreach (var value in data.vecs ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 4) continue;
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Vecs.Add(hash, Parse.VectorXZY(split, 1));
    }
    foreach (var value in data.quats ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length != 4) continue;
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.Quats.Add(hash, Parse.AngleYXZ(split, 1));
    }
    foreach (var value in data.bytes ?? new string[0])
    {
      var split = Parse.Split(value);
      if (split.Length < 2) continue;
      var str = string.Join(",", split.Skip(1));
      var hash = int.TryParse(split[0], out var h) ? h : split[0].GetStableHashCode();
      zdo.ByteArrays.Add(hash, Convert.FromBase64String(str));
    }
    return zdo;
  }
  public static DataData ToData(ZDOData zdo)
  {
    DataData data = new()
    {
      name = zdo.Name,
      floats = zdo.Floats.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value)}").ToArray(),
      ints = zdo.Ints.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value}").ToArray(),
      longs = zdo.Longs.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value}").ToArray(),
      strings = zdo.Strings.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value}").ToArray(),
      vecs = zdo.Vecs.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value)}").ToArray(),
      quats = zdo.Quats.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value)}").ToArray(),
      bytes = zdo.ByteArrays.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Convert.ToBase64String(pair.Value)}").ToArray(),
    };
    if (data.floats.Length == 0) data.floats = null;
    if (data.ints.Length == 0) data.ints = null;
    if (data.longs.Length == 0) data.longs = null;
    if (data.strings.Length == 0) data.strings = null;
    if (data.vecs.Length == 0) data.vecs = null;
    if (data.quats.Length == 0) data.quats = null;
    if (data.bytes.Length == 0) data.bytes = null;
    return data;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
  }
}
