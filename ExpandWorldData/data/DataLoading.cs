using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;
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
    yaml += "\n" + DataManager.Serializer().Serialize(new[] { ToData(data) });
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
    return [];
  }

  public static ZDOData FromData(DataData data)
  {
    ZDOData zdo = new()
    {
      Name = data.name
    };
    if (data.floats != null)
    {
      zdo.Floats ??= [];
      foreach (var value in data.floats)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : Helper.Hash(split[0]);
        zdo.Floats.Add(hash, new(split));
      }
    }
    if (data.ints != null)
    {
      zdo.Ints ??= [];
      foreach (var value in data.ints)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : Helper.Hash(split[0]);
        zdo.Ints.Add(hash, new(split));
      }
    }
    if (data.bools != null)
    {
      zdo.Ints ??= [];
      foreach (var value in data.bools)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Ints.Add(hash, new BoolValue(split));
      }
    }
    if (data.hashes != null)
    {
      zdo.Ints ??= [];
      foreach (var value in data.hashes)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Ints.Add(hash, new HashValue(split));
      }
    }
    if (data.longs != null)
    {
      zdo.Longs ??= [];
      foreach (var value in data.longs)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Longs.Add(hash, new(split));
      }
    }
    if (data.strings != null)
    {
      zdo.Strings ??= [];
      foreach (var value in data.strings)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Strings.Add(hash, new(split));
      }
    }
    if (data.vecs != null)
    {
      zdo.Vecs ??= [];
      foreach (var value in data.vecs)
      {
        var split = Parse.Split(value);
        if (split.Length != 4) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Vecs.Add(hash, Parse.VectorXZY(split, 1));
      }
    }
    if (data.quats != null)
    {
      zdo.Quats ??= [];
      foreach (var value in data.quats)
      {
        var split = Parse.Split(value);
        if (split.Length != 4) continue;
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.Quats.Add(hash, Parse.AngleYXZ(split, 1));
      }
    }
    if (data.bytes != null)
    {
      zdo.ByteArrays ??= [];
      foreach (var value in data.bytes)
      {
        var split = Parse.Split(value);
        if (split.Length < 2) continue;
        var str = string.Join(",", split.Skip(1));
        var hash = int.TryParse(split[0], out var h) ? h : DefaultData.Hash(split[0]);
        zdo.ByteArrays.Add(hash, Convert.FromBase64String(str));
      }
    }
    if (!string.IsNullOrWhiteSpace(data.connection))
    {
      var split = Parse.Split(data.connection);
      if (split.Length > 1)
      {
        var types = split.Take(split.Length - 1).ToList();
        var hash = split[split.Length - 1];
        zdo.ConnectionType = DataManager.ToByteEnum<ZDOExtraData.ConnectionType>(types);
        zdo.ConnectionHash = Parse.Int(hash);
        if (zdo.ConnectionHash == 0) zdo.ConnectionHash = hash.GetStableHashCode();
      }
    }
    return zdo;
  }
  public static DataData ToData(ZDOData zdo)
  {
    DataData data = new()
    {
      name = zdo.Name,
      floats = zdo.Floats?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value.Get())}").ToArray(),
      ints = zdo.Ints?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value.Get()}").ToArray(),
      longs = zdo.Longs?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value.Get()}").ToArray(),
      strings = zdo.Strings?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {pair.Value.Get()}").ToArray(),
      vecs = zdo.Vecs?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value)}").ToArray(),
      quats = zdo.Quats?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Helper.Print(pair.Value)}").ToArray(),
      bytes = zdo.ByteArrays?.Select(pair => $"{DefaultData.Convert(pair.Key)}, {Convert.ToBase64String(pair.Value)}").ToArray(),
    };
    if (zdo.ConnectionType != ZDOExtraData.ConnectionType.None && zdo.ConnectionHash != 0)
      data.connection = $"{zdo.ConnectionType}, {zdo.ConnectionHash}";
    return data;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
  }
}
