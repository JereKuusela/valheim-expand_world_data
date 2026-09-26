using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using Service;

namespace ExpandWorld.Drops;

public static class Loader
{
  public static readonly string ReferenceFileName = "ref_expand_drops.yaml";
  public static readonly string ReferenceFilePath = Path.Combine(Yaml.BaseDirectory, ReferenceFileName);
  public static readonly string FileName = "expand_drops.yaml";
  public static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public const string Pattern = "expand_drops*.yaml";

  public static void ToReferenceFile()
  {
    if (Helper.IsClient()) return;
    if (File.Exists(ReferenceFilePath)) return;
    ReferenceFileGenerator.Save();
  }

  public static void CreateConfig()
  {
    if (!Configuration.DataDrops || Helper.IsClient() || File.Exists(FilePath)) return;
    File.WriteAllText(FilePath, "# Drop data. See reference file for examples.");
  }

  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (!Configuration.DataDrops)
    {
      if (Set([]))
        Configuration.valueDropData.Value = "";
      return;
    }
    var files = DataManager.Read(Pattern);
    if (files == null || !Set(files)) return;
    Configuration.valueDropData.Value = string.Join("\n", files.Values);
  }

  public static Data FromData(Data data, string fileName)
  {
    data.biomes = DataManager.ToBiomes(data.biome, fileName);
    data.biomeAreas = DataManager.ToBiomeAreas(data.biomeArea, fileName);
    if (data.log.Equals("none", StringComparison.OrdinalIgnoreCase))
      data.logNone = true;
    else if (data.log != "")
      data.logObj = DataManager.ToPrefab(data.log, fileName);
    if (data.stump.Equals("none", StringComparison.OrdinalIgnoreCase))
      data.stumpNone = true;
    else if (data.stump != "")
      data.stumpObj = DataManager.ToPrefab(data.stump, fileName);
    foreach (var drop in data.drops)
    {
      drop.obj = DataManager.ToPrefab(drop.prefab, fileName);
      if (drop.obj)
        drop.item = drop.obj.GetComponent<ItemDrop>();
      drop.biomes = DataManager.ToBiomes(drop.biome, fileName);
      drop.biomeAreas = DataManager.ToBiomeAreas(drop.biomeArea, fileName);
    }
    return data;
  }
  public static bool IsValid(Data data) => data.drops.All(d => d.obj != null)
    && (data.log == "" || data.logNone || data.logObj != null)
    && (data.stump == "" || data.stumpNone || data.stumpObj != null);

  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }

  public static bool Set(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      Manager.DataByHash.Clear();
      Manager.DataByName.Clear();
      Patcher.SetEnabled(false);
      EWD.Instance.InvokeRegenerate();
      return true;
    }
    try
    {
      List<Data> data = [];
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<Data>(file.Value, file.Key, out var parsed)) return false;
        data.AddRange(parsed.Select(d => FromData(d, file.Key)).Where(IsValid));
      }
      if (data.Count == 0)
        return false;
      Manager.DataByHash.Clear();
      Manager.DataByName.Clear();
      foreach (var entry in data)
        Manager.Add(entry);
      Patcher.SetEnabled(true);
      Log.Info($"Reloading drop data ({data.Count} entries).");
      EWD.Instance.InvokeRegenerate();
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
    CreateConfig();
    if (Helper.IsServer()) ReadConfigs();
    else if (Configuration.valueDropData.Value != "") FromSetting(Configuration.valueDropData.Value);
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfigs);
}
