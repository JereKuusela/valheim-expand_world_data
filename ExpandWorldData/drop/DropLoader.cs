using System;
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

  public static void ReadConfig()
  {
    if (!Configuration.DataDrops || Helper.IsClient()) return;
    var yaml = DataManager.Read<Data, Data>(Pattern, FromData, out var hasFiles, out var hasData);
    if (hasFiles && !hasData) return;
    Configuration.valueDropData.Value = yaml;
    Set(yaml);
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
    ClientDataFlow.Apply(yaml, Configuration.DataDrops, Set);
  }

  public static void Set(string yaml)
  {
    Manager.DataByHash.Clear();
    Manager.DataByName.Clear();
    if (!Configuration.DataDrops || yaml == "") return;
    try
    {
      var data = Yaml.Deserialize<Data>(yaml, "Drops").Select(d => FromData(d, "Drops")).Where(IsValid).ToList();
      if (data.Count == 0)
      {
        // No errors as empty is ok.
        return;
      }
      foreach (var entry in data)
        Manager.Add(entry);
      Log.Info($"Reloading drop data ({data.Count} entries).");
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }

  public static void Toggle()
  {
    ExpandWorldData.Patcher.Update(EWD.Harmony);
    if (Helper.IsServer()) ReadConfig();
    else FromSetting(Configuration.valueDropData.Value);
  }

  internal static void InitializeData()
  {
    ToReferenceFile();
    CreateConfig();
    if (Helper.IsServer()) ReadConfig();
    else if (Configuration.valueDropData.Value != "") Set(Configuration.valueDropData.Value);
  }

  public static void SetupWatcher() => Yaml.SetupWatcher(Pattern, ReadConfig);
}
