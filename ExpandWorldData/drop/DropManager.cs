using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using Service;
using UnityEngine;

namespace ExpandWorld.Drops;

public class DropManager
{
  public static readonly string ReferenceFileName = "ref_expand_drops.yaml";
  public static readonly string ReferenceFilePath = Path.Combine(Yaml.BaseDirectory, ReferenceFileName);
  public static readonly string FileName = "expand_drops.yaml";
  public static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public const string Pattern = "expand_drops*.yaml";
  public static readonly int HashDrop = "ews_drops".GetStableHashCode();

  public static void ToReferenceFile()
  {
    if (Helper.IsClient()) return;
    if (File.Exists(ReferenceFilePath)) return;
    ReferenceFileGenerator.Save();
  }

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => DropManager.FilePath;
    protected override string Pattern => DropManager.Pattern;
    protected override bool DataEnabled => Configuration.DataDrops;
    protected override string ConfigValue { get => Configuration.valueDropData.Value; set => Configuration.valueDropData.Value = value; }
    protected override bool Set(Dictionary<string, string> files) => DropManager.Set(files);
    protected override void WriteDefaultConfig() => File.WriteAllText(FilePath, "# Drop data. See reference file for examples.");
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();

  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private static bool Set(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      DataByHash.Clear();
      DataByName.Clear();
      return true;
    }
    try
    {
      List<Data> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<Data>(file.Value, file.Key, out var parsed)) return false;
        data.AddRange(parsed.Select(d => Loader.FromData(d, file.Key)).Where(Loader.IsValid));
      }
      if (data.Count == 0)
        return false;
      DataByHash.Clear();
      DataByName.Clear();
      foreach (var entry in data)
        Add(entry);
      Log.Info($"Reloading drop data ({data.Count} entries).");
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return false;
    }
  }

  internal static void InitializeData()
  {
    ToReferenceFile();
    if (Helper.IsServer()) ReadConfigs();
    else if (Configuration.valueDropData.Value != "") FromSetting(Configuration.valueDropData.Value);
  }

  public static void SetupWatcher() => Yaml.SetupDataWatcher(Pattern, Configuration.configDataDrops, ReadConfigs);

  public static Dictionary<int, Data> DataByHash = [];
  public static Dictionary<string, Data> DataByName = [];
  public static bool HasData => DataByHash.Count > 0;

  public static bool Matches(Heightmap.Biome biomes, Heightmap.BiomeArea areas, Vector3 pos)
  {
    var biome = WorldGenerator.instance.GetBiome(pos);
    var area = WorldGenerator.instance.GetBiomeArea(pos);
    return biomes.HasFlag(biome) && areas.HasFlag(area);
  }

  public static void Add(Data data)
  {
    var hash = data.name.GetStableHashCode();
    DataByHash[hash] = data;
    DataByName[data.name] = data;
  }

  public static bool TryGetData(ZNetView view, out Data data)
  {
    if (!view)
    {
      data = null!;
      return false;
    }
    var zDO = view.GetZDO();
    if (zDO == null)
    {
      data = null!;
      return false;
    }
    return TryGetData(zDO, out data);
  }
  public static bool TryGetData(ZDO zDO, out Data data)
  {
    data = null!;
    if (zDO == null) return false;
    var hash = zDO.GetInt(HashDrop, 0);
    if (hash != 0)
      return DataByHash.TryGetValue(hash, out data);
    var name = zDO.GetString(HashDrop, "");
    if (name != "")
      return DataByName.TryGetValue(name, out data);
    return false;
  }
}
