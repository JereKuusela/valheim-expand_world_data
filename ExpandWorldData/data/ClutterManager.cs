using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace ExpandWorldData;

[HarmonyPatch]
public class ClutterManager
{
  public static string FileName = "expand_clutter.yaml";
  public static string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  public static string Pattern = "expand_clutter*.yaml";
  private static ClutterSystem.Clutter[] Originals = [];
  private static Dictionary<string, GameObject> Prefabs = [];
  static void LoadPrefabs()
  {
    if (!ZNet.instance) return;
    Prefabs = Helper.ToDict(ClutterSystem.instance.m_clutter, item => item.m_prefab.name, item => item.m_prefab);
    if (Helper.IsServer())
      Originals = [.. ClutterSystem.instance.m_clutter];
  }
  public static ClutterSystem.Clutter FromData(ClutterData data)
  {
    ClutterSystem.Clutter clutter = new();
    if (Prefabs.TryGetValue(data.prefab, out var prefab))
      clutter.m_prefab = prefab;
    else
      EWD.Log.LogWarning($"Failed to find clutter prefab {data.prefab}.");
    clutter.m_enabled = data.enabled;
    clutter.m_amount = data.amount;
    clutter.m_biome = DataManager.ToBiomes(data.biome);
    clutter.m_instanced = data.instanced;
    clutter.m_onUncleared = data.onUncleared;
    clutter.m_onCleared = data.onCleared;
    clutter.m_scaleMin = data.scaleMin;
    clutter.m_scaleMax = data.scaleMax;
    clutter.m_maxTilt = data.maxTilt;
    clutter.m_minTilt = data.minTilt;
    clutter.m_maxAlt = data.maxAltitude;
    clutter.m_minAlt = data.minAltitude;
    clutter.m_maxVegetation = data.maxVegetation;
    clutter.m_minVegetation = data.minVegetation;
    clutter.m_snapToWater = data.snapToWater;
    clutter.m_terrainTilt = data.terrainTilt;
    clutter.m_randomOffset = data.randomOffset;
    clutter.m_minOceanDepth = data.minOceanDepth;
    clutter.m_maxOceanDepth = data.maxOceanDepth;
    clutter.m_inForest = data.inForest;
    clutter.m_forestTresholdMin = data.forestTresholdMin;
    clutter.m_forestTresholdMax = data.forestTresholdMax;
    clutter.m_fractalScale = data.fractalScale;
    clutter.m_fractalOffset = data.fractalOffset;
    clutter.m_fractalTresholdMin = data.fractalThresholdMin;
    clutter.m_fractalTresholdMax = data.fractalThresholdMax;
    return clutter;
  }
  public static ClutterData ToData(ClutterSystem.Clutter clutter)
  {
    ClutterData data = new()
    {
      prefab = clutter.m_prefab.name,
      enabled = clutter.m_enabled,
      amount = clutter.m_amount,
      biome = DataManager.FromBiomes(clutter.m_biome),
      instanced = clutter.m_instanced,
      onUncleared = clutter.m_onUncleared,
      onCleared = clutter.m_onCleared,
      scaleMin = clutter.m_scaleMin,
      scaleMax = clutter.m_scaleMax,
      maxTilt = clutter.m_maxTilt,
      minTilt = clutter.m_minTilt,
      maxAltitude = clutter.m_maxAlt,
      minAltitude = clutter.m_minAlt,
      maxVegetation = clutter.m_maxVegetation,
      minVegetation = clutter.m_minVegetation,
      snapToWater = clutter.m_snapToWater,
      terrainTilt = clutter.m_terrainTilt,
      randomOffset = clutter.m_randomOffset,
      minOceanDepth = clutter.m_minOceanDepth,
      maxOceanDepth = clutter.m_maxOceanDepth,
      inForest = clutter.m_inForest,
      forestTresholdMin = clutter.m_forestTresholdMin,
      forestTresholdMax = clutter.m_forestTresholdMax,
      fractalScale = clutter.m_fractalScale,
      fractalOffset = clutter.m_fractalOffset,
      fractalThresholdMin = clutter.m_fractalTresholdMin,
      fractalThresholdMax = clutter.m_fractalTresholdMax
    };
    return data;
  }

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataClutter) return;
    if (File.Exists(FilePath)) return;
    var yaml = DataManager.Serializer().Serialize(ClutterSystem.instance.m_clutter.Select(ToData).ToList());
    File.WriteAllText(FilePath, yaml);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = Configuration.DataClutter ? DataManager.Read(Pattern) : "";
    Configuration.valueClutterData.Value = yaml;
    Set(yaml);
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  private static void Set(string yaml)
  {
    if (yaml == "" || !Configuration.DataClutter) return;
    if (Prefabs.Count == 0)
      LoadPrefabs();
    try
    {
      var data = DataManager.Deserialize<ClutterData>(yaml, FileName)
        .Select(FromData).Where(clutter => clutter.m_prefab).ToList();
      if (data.Count == 0)
      {
        EWD.Log.LogWarning($"Failed to load any clutter data.");
        return;
      }
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data))
      {
        // Watcher triggers reload.
        return;
      }
      EWD.Log.LogInfo($"Reloading clutter data ({data.Count} entries).");
      ClutterSystem.instance.m_clutter.Clear();
      foreach (var clutter in data)
        ClutterSystem.instance.m_clutter.Add(clutter);
      ClutterSystem.instance.ClearAll();
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
  }
  private static bool AddMissingEntries(List<ClutterSystem.Clutter> entries)
  {
    var missingKeys = Originals.Select(s => s.m_prefab.name).Distinct().ToHashSet();
    foreach (var entry in entries)
      missingKeys.Remove(entry.m_prefab.name);
    if (missingKeys.Count == 0) return false;
    var missing = Originals.Where(clutter => missingKeys.Contains(clutter.m_prefab.name)).ToList();
    EWD.Log.LogWarning($"Adding {missing.Count} missing clutters to the expand_clutter.yaml file.");
    foreach (var clutter in missing)
      EWD.Log.LogWarning(clutter.m_prefab.name);
    var yaml = File.ReadAllText(FilePath);
    var data = DataManager.Serializer().Serialize(missing.Select(ToData));
    // Directly appending is risky but necessary to keep comments, etc.
    yaml += "\n" + data;
    File.WriteAllText(FilePath, yaml);
    return true;
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, FromFile);
  }
}
