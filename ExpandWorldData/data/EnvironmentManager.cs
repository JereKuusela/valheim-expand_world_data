using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using Service;
using UnityEngine;
using Common;

namespace ExpandWorldData;

public partial class EnvironmentManager
{
  public static string FileName = "expand_environments.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_environments*.yaml";
  internal static Dictionary<string, EnvSetup> Originals = [];
  public static Dictionary<string, EnvironmentData> Extra = [];
  public static bool HasStatusEffects => Extra.Values.Any(data => data.statusEffects.Count > 0);

  public static void Initialize()
  {
    var newOriginals = LocationList.m_allLocationLists
      .Select(list => list.m_environments)
      .Append(EnvMan.instance.m_environments)
      .SelectMany(list => list).ToLookup(env => env.m_name, env => env).ToDictionary(kvp => kvp.Key, kvp => kvp.First());
    // Needs to be set once per world. This can be checked detected by checking location lists.
    if (newOriginals.Count > 0) Originals = newOriginals;
  }

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => EnvironmentManager.FilePath;
    protected override string Pattern => EnvironmentManager.Pattern;
    protected override bool DataEnabled => Configuration.DataEnvironments;
    protected override string ConfigValue { get => Configuration.valueEnvironmentData.Value; set => Configuration.valueEnvironmentData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => EnvironmentManager.Set(files);
    protected override void WriteDefaultConfig() => Save([.. Originals.Values], false);
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();
  public static void ReadConfigs() => Instance.ReadConfigs();
  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);

  private static SetResult Set(Dictionary<string, string> files)
  {
    var previousExtra = Extra;
    Extra = [];
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(files);
    }
    finally
    {
      if (result != SetResult.Applied) Extra = previousExtra;
    }
  }
  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    if (files.Count == 0)
    {
      if (Originals.Count == 0) return SetResult.Failed;
      Log.Info($"Reloading default environment data ({Originals.Count} entries).");
      Apply([.. Originals.Values]);
      return SetResult.Applied;
    }
    List<EnvSetup> data = [];
    foreach (var file in files)
    {
      if (!Yaml.TryDeserialize<EnvironmentYaml>(file.Value, file.Key, out var parsed)) return SetResult.Failed;
      data.AddRange(parsed.Select(d => FromData(d, file.Key)));
    }
    if (data.Count == 0)
    {
      Log.Warning($"Failed to load any environment data.");
      return SetResult.Failed;
    }
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(data))
      return SetResult.Rewritten;
    Log.Info($"Reloading environment data ({data.Count} entries).");
    Apply(data);
    return SetResult.Applied;
  }
  private static void Apply(List<EnvSetup> data)
  {
    foreach (var list in LocationList.m_allLocationLists)
      list.m_environments.Clear();
    var em = EnvMan.instance;
    em.m_environments.Clear();
    foreach (var env in data)
      em.AppendEnvironment(env);
    em.m_environmentPeriod = -1;
    em.m_firstEnv = true;
    foreach (var biome in em.m_biomes)
      em.InitializeBiomeEnvSetup(biome);
  }

  private static bool AddMissingEntries(List<EnvSetup> entries)
  {
    var missingKeys = Originals.Keys.ToHashSet();
    foreach (var entry in entries)
      missingKeys.Remove(entry.m_name);
    if (missingKeys.Count == 0) return false;
    var missing = Originals.Values.Where(env => missingKeys.Contains(env.m_name)).ToList();
    Log.Warning($"Adding {missing.Count} missing environments to the expand_environments.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<EnvSetup> data, bool log)
  {
    YamlFiles.SavePerMod("environments", data.Select(ToData).ToList(), item => item.name, log);
  }

  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataEnvironments, ReadConfigs);
  }
}
