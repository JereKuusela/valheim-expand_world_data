using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;
using Common;
using UnityEngine;
namespace ExpandWorldData;

public partial class ClutterManager
{
  public static string FileName = "expand_clutter.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_clutter*.yaml";
  private static readonly List<ClutterYaml> ExtraClutterYamls = [];
  private static List<ClutterSystem.Clutter> DefaultEntries = [];
  private static Dictionary<string, GameObject> Prefabs = [];

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => ClutterManager.FilePath;
    protected override string Pattern => ClutterManager.Pattern;
    protected override bool DataEnabled => Configuration.DataClutter;
    protected override string ConfigValue { get => Configuration.valueClutterData.Value; set => Configuration.valueClutterData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => ClutterManager.Set(files);
    protected override void WriteDefaultConfig() => SaveDefaultFile();
  }
  private static readonly Sync Instance = new();

  public static void AddClutter(ClutterYaml yaml)
  {
    ExtraClutterYamls.Add(yaml);
  }
  private static void SaveDefaultFile()
  {
    var data = DefaultEntries.Select(ToData).ToList();
    if (ExtraClutterYamls.Count > 0)
      data.AddRange(ExtraClutterYamls);
    Save(data, false);
  }

  public static void Initialize()
  {
    Prefabs = Helper.ToDict(ClutterSystem.instance.m_clutter, item => item.m_prefab.name, item => item.m_prefab);
    DefaultEntries = [.. ClutterSystem.instance.m_clutter];
  }

  public static void CreateConfigs() => Instance.CreateConfigs();
  public static void ReadConfigs() => Instance.ReadConfigs();
  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);
  public static SetResult Set(Dictionary<string, string> files)
  {
    var result = TryParseYaml(files, out var data);
    if (result != SetResult.Applied) return result;
    ClutterSystem.instance.m_clutter.Clear();
    foreach (var clutter in data)
      ClutterSystem.instance.m_clutter.Add(clutter);
    Refresh.Request(Regen.Clutter);
    return SetResult.Applied;
  }
  private static SetResult TryParseYaml(Dictionary<string, string> files, out List<ClutterSystem.Clutter> data)
  {
    if (files.Count == 0)
    {
      Log.Info($"Reloading default clutter data ({DefaultEntries.Count} entries).");
      data = DefaultEntries;
      return SetResult.Applied;
    }
    data = [];
    foreach (var file in files)
    {
      if (!Yaml.TryDeserialize<ClutterYaml>(file.Value, file.Key, out var parsed)) return SetResult.Failed;
      data.AddRange(parsed.Select(d => FromData(d, file.Key)).Where(clutter => clutter.m_prefab));
    }

    if (data.Count == 0)
    {
      Log.Warning($"Failed to load any clutter data.");
      return SetResult.Failed;
    }
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(data))
      return SetResult.Rewritten;
    Log.Info($"Reloading clutter data ({data.Count} entries).");
    return SetResult.Applied;
  }
  private static bool AddMissingEntries(List<ClutterSystem.Clutter> entries)
  {
    var missingKeys = DefaultEntries.Select(s => s.m_prefab.name).Distinct().ToHashSet();
    foreach (var entry in entries)
      missingKeys.Remove(entry.m_prefab.name);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(clutter => missingKeys.Contains(clutter.m_prefab.name)).Select(ToData).ToList();
    Log.Warning($"Adding {missing.Count} missing clutters to the expand_clutter.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<ClutterYaml> data, bool log)
  {
    YamlFiles.SavePerMod("clutter", data, item => item.prefab, log);
  }

  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataClutter, ReadConfigs);
  }
}
