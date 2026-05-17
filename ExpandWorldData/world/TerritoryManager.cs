using System;
using System.Collections.Generic;
using System.IO;
using Service;

namespace ExpandWorldData;

public class TerritoryManager
{
  public static string FileName = "expand_territories.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_territories*.yaml";

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
  public static bool TryGetData(string territory, out TerritoryData data) => Data.TryGetValue(Normalize(territory), out data);

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataTerritory) return;
    if (File.Exists(FilePath)) return;
    if (ExtraTerritoryYamls.Count == 0) return;
    var yaml = Yaml.Serializer().Serialize(ExtraTerritoryYamls.Values);
    File.WriteAllText(FilePath, yaml);
    FromFile();
  }

  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = Configuration.DataTerritory ? DataManager.Read<TerritoryYaml, TerritoryYaml>(Pattern, From) : "";
    Configuration.valueTerritoryData.Value = yaml;
    Set(yaml);
  }

  private static TerritoryYaml From(TerritoryYaml data, string file) => data;

  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }

  private static List<TerritoryYaml> Parse(string yaml)
  {
    List<TerritoryYaml> rawData = [];
    if (Configuration.DataTerritory)
    {
      try
      {
        rawData = Yaml.Deserialize<TerritoryYaml>(yaml, "Territories");
      }
      catch (Exception e)
      {
        Log.Warning("Failed to load any territory data.");
        Log.Error(e.Message);
        Log.Error(e.StackTrace);
      }
    }
    return rawData;
  }

  private static void Set(string yaml)
  {
    Data.Clear();
    if (yaml == "" || !Configuration.DataTerritory) return;
    var rawData = Parse(yaml);
    if (rawData.Count > 0)
      Log.Info($"Reloading territory data ({rawData.Count} entries).");

    foreach (var item in rawData)
    {
      var name = Normalize(item.territory);
      if (name == "") continue;
      var data = new TerritoryData(item);
      if (data.IsValid())
        Data[name] = data;
    }
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, FromFile);
  }

  private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}