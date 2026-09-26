using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData;

public class TerritoryManager
{
  public static string FileName = "expand_territories.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_territories*.yaml";
  private static bool Initialized;
  private static bool Pending;

  public static void Load()
  {
    Initialized = true;
    if (!Pending) return;
    Pending = false;
    FromSetting(Configuration.valueTerritoryData.Value);
  }

  public static void CleanUp()
  {
    Initialized = false;
    Pending = false;
  }

  private static readonly Dictionary<string, TerritoryYaml> ExtraTerritoryYamls = [];

  public static void AddTerritory(TerritoryYaml yaml)
  {
    var name = Normalize(yaml.territory);
    if (name == "")
      throw new Exception("Territory name can't be empty.");
    if (ExtraTerritoryYamls.ContainsKey(name))
      throw new Exception($"Territory {yaml.territory} already exists.");
    ExtraTerritoryYamls[name] = yaml;
  }

  private static readonly Dictionary<string, TerritoryData> Data = [];
  public static bool HasData => Data.Count > 0;
  public static bool TryGetData(string territory, out TerritoryData data) => Data.TryGetValue(Normalize(territory), out data);
  public static bool HasNoBuild => Data.Values.Any(data => data.noBuild);
  public static bool HasStatusEffects => Data.Values.Any(data => data.statusEffects.Count > 0);

  public static void CreateConfigs()
  {
    if (Helper.IsClient() || !Configuration.DataTerritory) return;
    if (File.Exists(FilePath)) return;
    if (ExtraTerritoryYamls.Count == 0) return;
    var yaml = Yaml.Serializer().Serialize(ExtraTerritoryYamls.Values);
    File.WriteAllText(FilePath, yaml);
  }

  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    if (Configuration.DataTerritory)
    {
      var files = DataManager.Read(Pattern);
      if (files == null || !Set(files)) return;
      Configuration.valueTerritoryData.Value = string.Join("\n", files.Values);
    }
    else if (Set([]))
      Configuration.valueTerritoryData.Value = "";
  }

  private static TerritoryYaml From(TerritoryYaml data, string file) => data;

  public static void FromSetting(string yaml)
  {
    if (!Helper.IsClient()) return;
    if (!Initialized) { Pending = true; return; }
    Set(yaml == "" ? [] : new() { ["synchronized"] = yaml });
  }

  private static bool Set(Dictionary<string, string> files)
  {
    if (!Yaml.TryDeserialize<TerritoryYaml>(files, out var rawData)) return false;
    Dictionary<string, TerritoryData> data = [];
    if (rawData.Count > 0)
    {
      Log.Info($"Reloading territory data ({rawData.Count} entries).");

      foreach (var item in rawData)
      {
        var name = Normalize(item.territory);
        if (name == "") continue;
        var entry = new TerritoryData(item);
        if (entry.IsValid())
          data[name] = entry;
      }
    }
    Data.Clear();
    foreach (var entry in data) Data[entry.Key] = entry.Value;
    World.Patcher.SetTerritoryEnabled(Data.Count > 0);
    EWD.Instance.InvokeRegenerate();
    return true;
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, ReadConfigs);
  }

  private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}