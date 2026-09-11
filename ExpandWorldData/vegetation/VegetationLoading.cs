using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Data;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class VegetationLoading
{
  private static readonly string FileName = "expand_vegetation.yaml";
  private static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  private static readonly string Pattern = "expand_vegetation*.yaml";
  private static readonly string AltBiomeDirectory = Path.Combine(Yaml.BaseDirectory, "AltBiomes");
  public static readonly int HashDrop = "ews_drops".GetStableHashCode();
  private static readonly List<VegetationYaml> ExtraVegetationYamls = [];
  private static readonly HashSet<string> CompleteAltBiomes = new(StringComparer.Ordinal);
  private static readonly FileReloadBatch ReloadBatch = new(ReadConfigs, GetFileSnapshot);

  public static void AddVegetation(VegetationYaml yaml)
  {
    ExtraVegetationYamls.Add(yaml);
  }


  // Default items are stored to track missing entries.
  private static List<ZoneSystem.ZoneVegetation> DefaultEntries = [];
  public static void Initialize()
  {
    ReloadBatch.Clear();
    DefaultEntries.Clear();
    DefaultKeys.Clear();
    if (Helper.IsServer())
      SetDefaultEntries();
  }

  public static void CreateConfigs()
  {
    if (Helper.IsClient()) return;
    if (!Configuration.DataVegetation) return;
    if (!File.Exists(FilePath))
    {
      ToFile();
      return;
    }
    SaveAltBiomes(ReadLegacyConfigs());
  }

  public static void ReadConfigs()
  {
    if (Helper.IsClient()) return;
    CreateConfigs();
    var snapshot = GetFileSnapshot();
    CleanUp();
    if (!Configuration.DataVegetation)
    {
      Apply(DefaultEntries);
      ReloadBatch.MarkLoaded(snapshot);
      return;
    }
    Apply(FromFile());
    // Snapshot before Apply: migration writes must still trigger a follow-up.
    ReloadBatch.MarkLoaded(snapshot);
  }

  public static void CleanUp()
  {
    VegetationSpawning.Extra.Clear();
    VegetationSpawning.Prefabs.Clear();
    CompleteAltBiomes.Clear();
    VegetationComposition.CleanUp();
  }

  internal static bool UsesCompleteAltBiome(string? name) =>
    name != null && CompleteAltBiomes.Contains(name);

  private static void Apply(List<ZoneSystem.ZoneVegetation> data)
  {
    ZoneSystem.instance.m_vegetation = DefaultEntries;
    if (!Configuration.DataVegetation)
    {
      Log.Info($"Reloading default vegetation data ({DefaultEntries.Count} entries).");
      return;
    }
    if (data.Count == 0)
    {
      Log.Warning($"Failed to load any vegetation data.");
      Log.Info($"Reloading default vegetation data ({DefaultEntries.Count} entries).");
      return;
    }
    if (Configuration.DataMigration && AddMissingEntries(data))
    {
      // Watcher triggers reload.
      return;
    }
    Log.Info($"Reloading vegetation data ({data.Count} entries).");
    ZoneSystem.instance.m_vegetation = data;
    VegetationComposition.Rebuild(data);
    IdManager.SendVegetationIds();

  }
  private static void ToFile()
  {
    var data = DefaultEntries
      .Where(entry => NormalizeAltBiome(entry.m_altBiomeParent) == null)
      .Select(ToData).ToList();
    data.AddRange(ExtraVegetationYamls.Where(entry => NormalizeAltBiome(entry.altBiome) == null));
    Save(data);
    SaveAltBiomes();
  }
  ///<summary>Loads all yaml files returning the deserialized vegetation entries.</summary>
  private static List<ZoneSystem.ZoneVegetation> FromFile()
  {
    try
    {
      RefreshCompleteAltBiomes();
      List<ZoneSystem.ZoneVegetation> result = [];
      foreach (var path in GetVegetationFiles().Reverse())
      {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var hasCompleteOwner = TryGetCompleteOwner(path, out var completeOwner);
        foreach (var entry in Yaml.Deserialize<VegetationYaml>(File.ReadAllText(path), fileName))
        {
          var owner = ResolveAltBiome(entry);
          if (hasCompleteOwner && owner != completeOwner)
          {
            Log.Warning($"{fileName}: Vegetation {entry.prefab} belongs to '{owner ?? "base vegetation"}' instead of '{completeOwner}' and was ignored.");
            continue;
          }
          // A complete file is authoritative for its owner. Ignoring owner
          // rows elsewhere prevents legacy and complete formats from stacking.
          if (!hasCompleteOwner && UsesCompleteAltBiome(owner))
            continue;
          var vegetation = FromData(entry, fileName);
          if (vegetation.m_prefab)
            result.Add(vegetation);
        }
      }
      return result;
    }
    catch (Exception e)
    {
      CompleteAltBiomes.Clear();
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    return [];
  }

  private static void SaveAltBiomes(List<VegetationYaml>? configuredEntries = null)
  {
    Directory.CreateDirectory(AltBiomeDirectory);
    foreach (var altBiome in AltBiomeList.m_altBiomes
      .Where(alt => !string.IsNullOrWhiteSpace(alt.m_name))
      .GroupBy(alt => alt.m_name, StringComparer.Ordinal)
      .Select(group => group.First()))
    {
      var path = GetAltBiomePath(altBiome.m_name);
      if (File.Exists(path)) continue;
      var inherited = configuredEntries == null
        ? DefaultEntries
          .Where(entry => NormalizeAltBiome(entry.m_altBiomeParent) == null)
          .Where(entry => (entry.m_biome & altBiome.m_biome) != 0)
          .Select(ToData).ToList()
        : configuredEntries
          .Where(entry => NormalizeAltBiome(entry.altBiome) == null)
          .Where(entry => (DataManager.ToBiomes(entry.biome, FileName) & altBiome.m_biome) != 0)
          .Select(Clone).ToList();
      foreach (var entry in inherited)
        entry.altBiome = altBiome.m_name;

      var additions = configuredEntries?
        .Where(entry => NormalizeAltBiome(entry.altBiome) == altBiome.m_name)
        .Select(Clone).ToList() ?? [];
      var configuredPrefabs = additions
        .SelectMany(entry => DataManager.ToList(entry.prefab))
        .ToHashSet(StringComparer.Ordinal);
      additions.AddRange(DefaultEntries
        .Where(entry => NormalizeAltBiome(entry.m_altBiomeParent) == altBiome.m_name)
        .Where(entry => !configuredPrefabs.Contains(entry.m_prefab.name))
        .Select(ToData));
      additions.AddRange(ExtraVegetationYamls
        .Where(entry => NormalizeAltBiome(entry.altBiome) == altBiome.m_name));

      var blocked = inherited.Concat(additions)
        .Where(entry => altBiome.m_blockVegetationNames.Contains(entry.prefab))
        .ToList();
      inherited = inherited.Except(blocked).ToList();
      additions = additions.Except(blocked).ToList();
      foreach (var entry in blocked)
        entry.enabled = false;

      var parent = DataManager.FromBiomes(altBiome.m_biome);
      var yaml = $"# Complete vegetation for the {altBiome.m_name} alternate biome.\n" +
        $"# Parent biome: {parent}. Edit this file as one complete biome.\n" +
        "# Disable an entry with enabled: false instead of removing it.\n\n" +
        $"# Inherited from {parent}\n" + SerializeSection(inherited) + "\n" +
        $"# Added by {altBiome.m_name}\n" + SerializeSection(additions) + "\n" +
        $"# Disabled by {altBiome.m_name}\n" + SerializeSection(blocked);
      File.WriteAllText(path, yaml);
    }
  }

  private static string SerializeSection(List<VegetationYaml> data) =>
    data.Count == 0 ? "# None\n" : Yaml.Serializer().Serialize(data);

  private static VegetationYaml Clone(VegetationYaml entry) =>
    Yaml.Deserializer().Deserialize<VegetationYaml>(Yaml.Serializer().Serialize(entry));

  private static List<VegetationYaml> ReadLegacyConfigs()
  {
    List<VegetationYaml> result = [];
    foreach (var path in GetVegetationFiles().Where(path => !TryGetCompleteOwner(path, out _)).Reverse())
    {
      var fileName = Path.GetFileNameWithoutExtension(path);
      result.AddRange(Yaml.Deserialize<VegetationYaml>(File.ReadAllText(path), fileName));
    }
    return result;
  }

  private static IEnumerable<string> GetVegetationFiles()
  {
    if (!Directory.Exists(Yaml.BaseDirectory))
      Directory.CreateDirectory(Yaml.BaseDirectory);
    return Directory.GetFiles(Yaml.BaseDirectory, Pattern, SearchOption.AllDirectories);
  }

  private static bool TryGetCompleteOwner(string path, out string owner)
  {
    var fullPath = Path.GetFullPath(path);
    foreach (var altBiome in AltBiomeList.m_altBiomes)
    {
      if (string.IsNullOrWhiteSpace(altBiome.m_name)) continue;
      if (!string.Equals(fullPath, Path.GetFullPath(GetAltBiomePath(altBiome.m_name)), StringComparison.OrdinalIgnoreCase)) continue;
      owner = altBiome.m_name;
      return true;
    }
    owner = "";
    return false;
  }

  private static void RefreshCompleteAltBiomes()
  {
    CompleteAltBiomes.Clear();
    foreach (var altBiome in AltBiomeList.m_altBiomes)
      if (!string.IsNullOrWhiteSpace(altBiome.m_name) && File.Exists(GetAltBiomePath(altBiome.m_name)))
        CompleteAltBiomes.Add(altBiome.m_name);
  }

  private static string GetAltBiomePath(string name) =>
    Path.Combine(AltBiomeDirectory, $"expand_vegetation_{GetFileToken(name)}.yaml");

  private static string GetFileToken(string name)
  {
    var token = string.Concat(name.Select(character =>
      char.IsLetterOrDigit(character) || character == '-' ? character : '_')).Trim('_');
    while (token.Contains("__")) token = token.Replace("__", "_");
    return token == "" ? "UnnamedAltBiome" : token;
  }
  ///<summary>Cleans up default vegetation data and stores it to track missing entries.</summary>
  private static void SetDefaultEntries()
  {
    ZoneSystem.instance.m_vegetation = ZoneSystem.instance.m_vegetation
      .Where(veg => veg.m_prefab)
      .Where(veg => ZNetScene.instance.m_namedPrefabs.ContainsKey(veg.m_prefab.name.GetStableHashCode()))
      .Where(veg => veg.m_enable && veg.m_max > 0f).ToList();
    DefaultEntries = ZoneSystem.instance.m_vegetation;
    DefaultKeys = Helper.ToSet(DefaultEntries, GetMigrationKey);
  }
  // Used to optimize missing entries check (to avoid n^2 loop).
  // The alternate biome owner is part of the identity because Deep North can
  // register both base and alternate-biome rows for the same prefab.
  private static HashSet<string> DefaultKeys = [];

  private static string GetMigrationKey(ZoneSystem.ZoneVegetation vegetation) =>
    GetMigrationKey(vegetation.m_prefab.name, vegetation.m_altBiomeParent);

  private static string GetMigrationKey(string prefab, string? altBiome) =>
    prefab + "\0" + (NormalizeAltBiome(altBiome) ?? "");

  private static string? NormalizeAltBiome(string? altBiome) =>
    string.IsNullOrWhiteSpace(altBiome) ? null : altBiome;

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  // Note: This is needed people add new content mods and then complain that Expand World doesn't spawn them.
  private static bool AddMissingEntries(List<ZoneSystem.ZoneVegetation> entries)
  {
    var missingKeys = DefaultKeys.ToHashSet();
    // Some mods override prefabs so the m_prefab.name is not reliable.
    foreach (var entry in entries)
    {
      missingKeys.Remove(GetMigrationKey(entry.m_name, entry.m_altBiomeParent));
      if (VegetationSpawning.Prefabs.TryGetValue(entry, out var prefabs))
        foreach (var prefab in prefabs)
          missingKeys.Remove(GetMigrationKey(prefab.name, entry.m_altBiomeParent));
    }
    if (missingKeys.Count == 0) return false;
    // But don't use m_name because it can be anything for original items.
    var missing = DefaultEntries.Where(veg => missingKeys.Contains(GetMigrationKey(veg))).Select(ToData).ToList();
    Log.Warning($"Adding {missing.Count} missing vegetation to the vegetation configuration.");
    foreach (var item in missing)
      Log.Warning($"{AssetTracker.GetModFromPrefab(item.prefab)}: {item.prefab}");
    SaveMissing(missing);
    return true;
  }

  private static void SaveMissing(List<VegetationYaml> data)
  {
    var regular = data
      .Where(entry => !UsesCompleteAltBiome(NormalizeAltBiome(entry.altBiome)))
      .ToList();
    if (regular.Count > 0)
      Save(regular);

    foreach (var group in data
      .Where(entry => UsesCompleteAltBiome(NormalizeAltBiome(entry.altBiome)))
      .GroupBy(entry => NormalizeAltBiome(entry.altBiome)!))
    {
      var path = GetAltBiomePath(group.Key);
      var yaml = File.ReadAllText(path);
      if (!yaml.EndsWith("\n")) yaml += "\n";
      yaml += Yaml.Serializer().Serialize(group.ToList());
      File.WriteAllText(path, yaml);
    }
  }
  private static void Save(List<VegetationYaml> data)
  {
    Dictionary<string, List<VegetationYaml>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.prefab);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_vegetation{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value);
      File.WriteAllText(file, yaml);
    }
  }

  public static ZoneSystem.ZoneVegetation FromData(VegetationYaml data, string fileName)
  {
    if (data.minDistance > 0f)
      data.minDistance = WorldEntry.ConvertDist(data.minDistance);
    if (data.maxDistance > 0f)
      data.maxDistance = WorldEntry.ConvertDist(data.maxDistance);
    var altBiome = ResolveAltBiome(data);
    ZoneSystem.ZoneVegetation veg = new()
    {
      m_name = data.prefab,
      m_enable = data.enabled,
      m_min = data.min,
      m_max = data.max,
      m_forcePlacement = data.forcePlacement,
      m_scaleMin = Parse.Scale(data.scaleMin).x,
      m_scaleMax = Parse.Scale(data.scaleMax).x,
      m_randTilt = data.randTilt,
      m_chanceToUseGroundTilt = data.chanceToUseGroundTilt,
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_altBiomeParent = string.IsNullOrWhiteSpace(altBiome) ? null! : altBiome,
      m_biomeArea = DataManager.ToBiomeAreas(data.biomeArea, fileName),
      m_blockCheck = data.blockCheck,
      m_minAltitude = data.minAltitude,
      m_maxAltitude = data.maxAltitude,
      m_minOceanDepth = data.minOceanDepth,
      m_maxOceanDepth = data.maxOceanDepth,
      m_minVegetation = data.minVegetation,
      m_maxVegetation = data.maxVegetation,
      m_surroundCheckVegetation = data.surroundCheckVegetation,
      m_surroundCheckDistance = data.surroundCheckDistance,
      m_surroundCheckLayers = data.surroundCheckLayers,
      m_surroundBetterThanAverage = data.surroundBetterThanAverage,
      m_minTilt = data.minTilt,
      m_maxTilt = data.maxTilt,
      m_terrainDeltaRadius = data.terrainDeltaRadius,
      m_maxTerrainDelta = data.maxTerrainDelta,
      m_minTerrainDelta = data.minTerrainDelta,
      m_snapToWater = data.snapToWater,
      m_snapToStaticSolid = data.snapToStaticSolid,
      m_groundOffset = data.groundOffset,
      m_groupSizeMin = data.groupSizeMin,
      m_groupSizeMax = data.groupSizeMax,
      m_groupRadius = data.groupRadius,
      m_inForest = data.inForest,
      m_forestTresholdMin = data.forestTresholdMin,
      m_forestTresholdMax = data.forestTresholdMax,
      m_minDistanceFromCenter = data.minDistance,
      m_maxDistanceFromCenter = data.maxDistance,
    };
    Range<Vector3> scale = new(Parse.Scale(data.scaleMin), Parse.Scale(data.scaleMax))
    {
      Uniform = data.scaleUniform
    };
    VegetationExtra extra = new()
    {
      clearRadius = data.clearRadius,
      clearArea = data.clearArea,
    };
    // Minor optimization to skip RNG calls if there is nothing to randomize.
    if (Helper.IsMultiAxis(scale))
      extra.scale = scale;
    if (data.data != "")
      extra.data = DataHelper.Get(data.data, fileName);
    if (data.drops != "")
    {
      DataEntry entry = new()
      {
        Hashes = new Dictionary<int, IHashValue> { { HashDrop, DataValue.Hash(data.drops) } }
      };
    }


    var prefabs = DataManager.ToList(data.prefab).Select(p =>
    {
      var hash = p.GetStableHashCode();
      if (ZNetScene.instance.m_namedPrefabs.TryGetValue(hash, out var obj))
        return obj;
      if (BlueprintManager.Load(data.prefab))
        return new(data.prefab);
      return null!;
    }).Where(p => p).ToList();


    if (prefabs.Count > 0)
      veg.m_prefab = prefabs[0];
    if (prefabs.Count > 1)
      VegetationSpawning.Prefabs.Add(veg, prefabs);

    if (veg.m_enable)
    {
      if (data.requiredGlobalKey != "")
        extra.requiredGlobalKeys = DataManager.ToList(data.requiredGlobalKey);
      if (data.forbiddenGlobalKey != "")
        extra.forbiddenGlobalKeys = DataManager.ToList(data.forbiddenGlobalKey);
      if (data.centerX != 0f || data.centerY != 0f)
      {
        // Center is not supported in the original game, so to have to fallback to the custom check.
        veg.m_minDistanceFromCenter = 0;
        veg.m_maxDistanceFromCenter = 0;
        extra.center = new(WorldEntry.ConvertDist(data.centerX), WorldEntry.ConvertDist(data.centerY));
        if (data.minDistance != 0f || data.maxDistance != 0f)
          extra.distance = new(data.minDistance, data.maxDistance);
      }
    }
    if (extra.IsValid())
      VegetationSpawning.Extra.Add(veg, extra);
    return veg;
  }

  private static string? ResolveAltBiome(VegetationYaml data)
  {
    // Explicit blank means base-biome vegetation. This is distinct from a
    // legacy omitted field, which can still inherit an unambiguous native owner.
    if (data.altBiome != null)
      return NormalizeAltBiome(data.altBiome);

    var prefabNames = DataManager.ToList(data.prefab);
    if (prefabNames.Count != 1)
      return null;

    var nativeOwners = DefaultEntries
      .Where(entry => entry.m_prefab && entry.m_prefab.name == prefabNames[0])
      .Select(entry => NormalizeAltBiome(entry.m_altBiomeParent))
      .Distinct()
      .ToList();

    if (nativeOwners.Count == 1)
      return nativeOwners[0];

    // When both base and alternate rows exist, an omitted field must resolve
    // to the base row. Picking the first native row made ordinary vegetation
    // dependent on registration order and could empty the base biome.
    if (nativeOwners.Any(owner => owner == null))
      return null;

    if (nativeOwners.Count > 1)
      Log.Warning($"Vegetation {data.prefab} has multiple alternate biome owners. Add altBiome explicitly to select one.");

    return null;
  }
  public static VegetationYaml ToData(ZoneSystem.ZoneVegetation veg)
  {
    VegetationYaml data = new()
    {
      enabled = veg.m_enable,
      prefab = veg.m_prefab.name,
      min = veg.m_min,
      max = veg.m_max,
      forcePlacement = veg.m_forcePlacement,
      scaleMin = veg.m_scaleMin.ToString(NumberFormatInfo.InvariantInfo),
      scaleMax = veg.m_scaleMax.ToString(NumberFormatInfo.InvariantInfo),
      randTilt = veg.m_randTilt,
      chanceToUseGroundTilt = veg.m_chanceToUseGroundTilt,
      biome = DataManager.FromBiomes(veg.m_biome),
      altBiome = veg.m_altBiomeParent ?? "",
      biomeArea = DataManager.FromBiomeAreas(veg.m_biomeArea),
      blockCheck = veg.m_blockCheck,
      minAltitude = veg.m_minAltitude,
      maxAltitude = veg.m_maxAltitude,
      minOceanDepth = veg.m_minOceanDepth,
      maxOceanDepth = veg.m_maxOceanDepth,
      minVegetation = veg.m_minVegetation,
      maxVegetation = veg.m_maxVegetation,
      minTilt = veg.m_minTilt,
      maxTilt = veg.m_maxTilt,
      terrainDeltaRadius = veg.m_terrainDeltaRadius,
      maxTerrainDelta = veg.m_maxTerrainDelta,
      minTerrainDelta = veg.m_minTerrainDelta,
      snapToWater = veg.m_snapToWater,
      snapToStaticSolid = veg.m_snapToStaticSolid,
      groundOffset = veg.m_groundOffset,
      groupSizeMin = veg.m_groupSizeMin,
      groupSizeMax = veg.m_groupSizeMax,
      groupRadius = veg.m_groupRadius,
      inForest = veg.m_inForest,
      forestTresholdMin = veg.m_forestTresholdMin,
      forestTresholdMax = veg.m_forestTresholdMax,
      surroundCheckVegetation = veg.m_surroundCheckVegetation,
      surroundCheckDistance = veg.m_surroundCheckDistance,
      surroundCheckLayers = veg.m_surroundCheckLayers,
      surroundBetterThanAverage = veg.m_surroundBetterThanAverage,
      maxDistance = veg.m_maxDistanceFromCenter / 10000f,
      minDistance = veg.m_minDistanceFromCenter / 10000f,
    };
    return data;
  }

  // The watcher runs on BepInEx's main-thread synchronizer. File bursts are
  // applied once after a quiet period; notifications for already-loaded defaults
  // are ignored by content, without suppressing later user edits.
  public static void UpdateReload(float deltaTime)
  {
    if (!ZNet.instance || !ZoneSystem.instance || Helper.IsClient()) return;
    try
    {
      ReloadBatch.Update(deltaTime);
    }
    catch (IOException error)
    {
      Log.Warning($"Unable to read vegetation files: {error.Message}");
    }
  }

  public static void ClearReload() => ReloadBatch.Clear();

  private static string GetFileSnapshot()
  {
    using var hash = SHA256.Create();
    var files = new StringBuilder();
    foreach (var path in GetVegetationFiles().OrderBy(path => path, StringComparer.Ordinal))
    {
      files.Append(path).Append('\0');
      files.Append(Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)))).Append('\n');
    }
    return files.ToString();
  }

  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, ReloadBatch.Notify);
  }
}
