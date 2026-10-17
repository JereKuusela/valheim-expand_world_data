using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using Common;
using UnityEngine;

namespace ExpandWorldData;

public class BiomeManager
{
  public static string FileName = "expand_biomes.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_biomes*.yaml";
  public static Dictionary<string, EnvironmentData> Extra = [];

  public static Dictionary<EnvEntry, EnvEntryKeys> EnvKeys = [];
  public static bool HasEnvironmentRules => EnvKeys.Count > 0;
  public static bool HasData { get; private set; }

  public static Heightmap.Biome LavaBiomes = Heightmap.Biome.AshLands;
  // Minor optimization to skip terrain color based calculations.
  public static Heightmap.Biome FullLavaBiomes = Heightmap.Biome.AshLands;
  public static Heightmap.Biome NoBuildBiomes = 0;
  public static bool HasLavaOverrides => HasData && (LavaBiomes != Heightmap.Biome.AshLands || FullLavaBiomes != Heightmap.Biome.AshLands);
  private static HashSet<string> UsedGlobalKeys = [];
  public static bool HasGlobalKeyRules => UsedGlobalKeys.Count > 0;

  public static EnvEntry FromData(BiomeEnvironment data, Dictionary<EnvEntry, EnvEntryKeys> keys)
  {
    EnvEntry env = new()
    {
      m_environment = data.environment,
      m_weight = data.weight,
      m_ashlandsOverride = data.ashlandsOverride,
      m_deepnorthOverride = data.deepNorthOverride
    };
    EnvEntryKeys key = new(data);
    if (key.HasKeys())
      keys[env] = key;
    return env;
  }
  public static BiomeEnvironment ToData(EnvEntry env)
  {
    return new()
    {
      environment = env.m_environment,
      weight = env.m_weight,
      ashlandsOverride = env.m_ashlandsOverride,
      deepNorthOverride = env.m_deepnorthOverride
    };
  }
  private static readonly Dictionary<string, Heightmap.Biome> OriginalBiomes = new() {
    { "None", Heightmap.Biome.None},
    { "Meadows", Heightmap.Biome.Meadows},
    { "Swamp", Heightmap.Biome.Swamp},
    { "Mountain", Heightmap.Biome.Mountain},
    { "BlackForest", Heightmap.Biome.BlackForest},
    { "Plains", Heightmap.Biome.Plains},
    { "AshLands", Heightmap.Biome.AshLands},
    { "DeepNorth", Heightmap.Biome.DeepNorth},
    { "Ocean", Heightmap.Biome.Ocean},
    { "Mistlands", Heightmap.Biome.Mistlands},
  };
  private static readonly Dictionary<string, BiomeYaml> ExtraBiomeYamls = [];
  public static void AddBiome(BiomeYaml yaml)
  {
    var name = yaml.name;
    if (ExtraBiomeYamls.ContainsKey(name))
      throw new Exception($"Biome {name} already exists.");
    ExtraBiomeYamls[name] = yaml;
  }
  ///<summary>Lower case biome names for easier data loading.</summary>
  private static Dictionary<string, Heightmap.Biome> NameToBiome = OriginalBiomes.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
  ///<summary>Original biome names because some mods rely on Enum.GetName(s) returning uppercase values.</summary>
  public static Dictionary<Heightmap.Biome, string> BiomeToDisplayName = OriginalBiomes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
  private static Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToTerrain = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
  private static Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToNature = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
  private static Dictionary<Heightmap.Biome, Color> BiomeToColor = [];
  private static Dictionary<Heightmap.Biome, BiomeData> BiomeData = [];
  public static bool TryGetColor(Heightmap.Biome biome, out Color color) => BiomeToColor.TryGetValue(biome, out color);
  public static bool TryGetData(Heightmap.Biome biome, out BiomeData data) => BiomeData.TryGetValue(biome, out data);
  public static bool HasNatureOverrides => BiomeToNature.Any(pair => pair.Key != pair.Value);
  public static bool HasCustomBiomes => NameToBiome.Count > OriginalBiomes.Count;
  public static bool HasStatusEffects => BiomeData.Values.Any(data => data.statusEffects.Count > 0);
  public static bool TryGetBiome(string name, out Heightmap.Biome biome) => NameToBiome.TryGetValue(name.ToLowerInvariant(), out biome);
  public static Heightmap.Biome GetBiome(string name) => NameToBiome.TryGetValue(name.ToLowerInvariant(), out var biome) ? biome : Heightmap.Biome.None;
  public static bool TryGetDisplayName(Heightmap.Biome biome, out string name) => BiomeToDisplayName.TryGetValue(biome, out name);
  public static Heightmap.Biome GetTerrain(Heightmap.Biome biome) => BiomeToTerrain.TryGetValue(biome, out var terrain) ? terrain : biome;
  public static Heightmap.Biome GetNature(Heightmap.Biome biome) => BiomeToNature.TryGetValue(biome, out var nature) ? nature : biome;
  public static BiomeEnvSetup FromData(BiomeYaml data, Dictionary<EnvEntry, EnvEntryKeys> keys, string fileName)
  {
    BiomeEnvSetup biome = new()
    {
      m_biome = DataManager.ToBiomes(data.biome, fileName),
      m_environments = data.environments == null || data.environments.Length == 0
        ? [new EnvEntry() { m_environment = "Clear", m_weight = 1f }]
        : [.. data.environments.Select(d => FromData(d, keys))],
      m_musicMorning = data.musicMorning,
      m_musicEvening = data.musicEvening,
      m_musicDay = data.musicDay,
      m_musicNight = data.musicNight
    };
    return biome;
  }
  public static BiomeYaml ToData(BiomeEnvSetup biome)
  {
    return new()
    {
      biome = DataManager.FromBiomes(biome.m_biome),
      environments = [.. biome.m_environments.Select(ToData)],
      musicMorning = biome.m_musicMorning,
      musicEvening = biome.m_musicEvening,
      musicDay = biome.m_musicDay,
      musicNight = biome.m_musicNight,
      colorTerrain = DataManager.FromColor(Heightmap.GetBiomeColor(biome.m_biome)),
      colorMap = DataManager.FromColor(Minimap.instance.GetPixelColor(biome.m_biome)),
      lava = biome.m_biome == Heightmap.Biome.AshLands ? "true" : ""
    };
  }
  public static void CreateConfigs() => Instance.CreateConfigs();
  // Set when the default file lacks environments, which can only be filled once EnvMan exists.
  // Load sets it from file contents; writing a partial default sets it too, since Load doesn't run when disabled.
  private static bool MissingEnvironments = false;
  public static void CompleteDefaultConfig()
  {
    if (Helper.IsClient() || !MissingEnvironments) return;
    MissingEnvironments = false;
    WriteDefaultConfig();
    Yaml.IgnoreWrites(Pattern);
    ReadConfigs();
  }
  // Phase 1 runs before EnvMan exists and writes only what world generation needs.
  private static BiomeYaml ToPartialData(Heightmap.Biome biome) => new()
  {
    biome = BiomeToDisplayName[biome],
    colorTerrain = DataManager.FromColor(Heightmap.GetBiomeColor(biome)),
    lava = biome == Heightmap.Biome.AshLands ? "true" : ""
  };
  private static void WriteDefaultConfig()
  {
    List<BiomeYaml> data;
    if (EnvMan.instance == null || Minimap.instance == null)
    {
      MissingEnvironments = true;
      data = [.. OriginalBiomes.Values.Where(b => b != Heightmap.Biome.None).Select(ToPartialData), .. ExtraBiomeYamls.Values];
    }
    else
    {
      var biomes = OriginalBiomes.Values.Select(b => EnvMan.instance.m_biomes.Find(ev => ev.m_biome == b)).Where(b => b != null);
      data = [.. biomes.Select(ToData), .. ExtraBiomeYamls.Values];
    }
    File.WriteAllText(FilePath, Yaml.Serializer().Serialize(data));
  }

  public static void ReadConfigs() => Instance.ReadConfigs();
  public static void NamesFromFile()
  {
    if (!Configuration.DataBiome) return;
    LoadNames(DataManager.ReadData<BiomeYaml, BiomeYaml>(Pattern, From));
  }
  private static BiomeYaml From(BiomeYaml data, string file) => data;
  public static void FromSetting(string yaml) => Instance.FromSetting(yaml);
  public static bool BiomeForestMultiplier = false;

  private static List<BiomeYaml> Parse(string yaml)
  {
    try
    {
      return Yaml.Deserialize<BiomeYaml>(yaml, "Biomes");
    }
    catch (Exception e)
    {
      Log.Warning("Failed to load any biome data.");
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
      return [];
    }
  }
  public static void SetNames(Dictionary<Heightmap.Biome, string> names)
  {
    BiomeToDisplayName = names;
    NameToBiome = BiomeToDisplayName.ToDictionary(kvp => kvp.Value.ToLowerInvariant(), kvp => kvp.Key);
    Log.Info($"Received {BiomeToDisplayName.Count} biome names.");
  }
  private static void LoadNames(List<BiomeYaml> rawData)
  {
    if (rawData.Count > 0)
      Log.Info($"Preloading biome names ({rawData.Count} entries).");
    var originalNames = OriginalBiomes.Select(kvp => kvp.Key.ToLowerInvariant()).ToHashSet();
    BiomeToDisplayName = OriginalBiomes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    var biome = Heightmap.Biome.Mistlands;
    foreach (var item in rawData)
    {
      if (originalNames.Contains(item.biome.ToLowerInvariant())) continue;
      biome = NextBiome(biome);
      BiomeToDisplayName[biome] = item.biome;

    }
    NameToBiome = BiomeToDisplayName.ToDictionary(kvp => kvp.Value.ToLowerInvariant(), kvp => kvp.Key);
  }
  private static List<BiomeEnvSetup> Environments = [];

  private class Sync : SyncedDataManager
  {
    protected override string FilePath => BiomeManager.FilePath;
    protected override string Pattern => BiomeManager.Pattern;
    protected override bool DataEnabled => Configuration.DataBiome;
    protected override string ConfigValue { get => Configuration.valueBiomeData.Value; set => Configuration.valueBiomeData.Value = value; }
    protected override SetResult Set(Dictionary<string, string> files) => BiomeManager.Set(files);
    protected override void WriteDefaultConfig() => BiomeManager.WriteDefaultConfig();
  }
  private static readonly Sync Instance = new();

  private static void Load(List<BiomeYaml> rawData, Dictionary<Heightmap.Biome, string> translations)
  {
    MissingEnvironments = false;
    HasData = rawData.Count > 0;
    ResetData();
    if (!HasData) return;
    Log.Info($"Reloading biome data ({rawData.Count} entries).");
    LavaBiomes = 0;
    FullLavaBiomes = 0;
    var lastBiome = Heightmap.Biome.Mistlands;

    foreach (var item in rawData)
    {
      var biome = lastBiome;
      if (NameToBiome.TryGetValue(item.biome.ToLowerInvariant(), out var defaultBiome))
      {
        biome = defaultBiome;
        if (item.name != "")
          translations[biome] = item.name;
      }
      else
      {
        biome = lastBiome = NextBiome(lastBiome);
        NameToBiome.Add(item.biome.ToLowerInvariant(), biome);
        BiomeToDisplayName[biome] = item.biome;
        translations[biome] = item.name;
      }
      BiomeData extra = new(item);
      if (extra.lava)
        LavaBiomes |= biome;
      if (extra.lava && extra.colorTerrain.a == 1 && extra.colorTerrain.r == 1)
        FullLavaBiomes |= biome;
      if (extra.noBuild)
        NoBuildBiomes |= biome;
      if (extra.IsValid())
        BiomeData[biome] = extra;
      if (item.paint != "") BiomeToColor[biome] = Terrain.ParsePaint(item.paint);

    }
    BiomeToTerrain = rawData.ToDictionary(data => GetBiome(data.biome), data =>
    {
      if (TryGetBiome(data.terrain, out var terrain))
        return terrain;
      return GetBiome(data.biome);
    });
    BiomeToNature = rawData.ToDictionary(data => GetBiome(data.biome), data =>
    {
      if (TryGetBiome(data.nature, out var nature))
        return nature;
      if (TryGetBiome(data.terrain, out var terrain))
        return terrain;
      return GetBiome(data.biome);
    });
    BiomeForestMultiplier = rawData.Any(data => data.forestMultiplier != 1f);
    var vanilla = rawData.Where(d => OriginalBiomes.Keys.Any(k => string.Equals(k, d.biome, StringComparison.OrdinalIgnoreCase))).ToList();
    MissingEnvironments = vanilla.Count > 0 && vanilla.All(d => d.environments == null);
    Environments = [.. rawData.Select(d => FromData(d, EnvKeys, "Biomes"))];
    UsedGlobalKeys = [.. EnvKeys.Values
      .SelectMany(data => data.requiredGlobalKeys.Concat(data.forbiddenGlobalKeys))
      .Where(key => key != null && key != "")
      .Select(NormalizeKey)];
  }
  private static void ResetData()
  {
    EnvKeys = [];
    UsedGlobalKeys = [];
    BiomeData = [];
    BiomeToColor = [];
    Environments = [];
    LavaBiomes = Heightmap.Biome.AshLands;
    FullLavaBiomes = Heightmap.Biome.AshLands;
    NoBuildBiomes = 0;
    BiomeForestMultiplier = false;
    NameToBiome = OriginalBiomes.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
    BiomeToDisplayName = OriginalBiomes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    BiomeToTerrain = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
    BiomeToNature = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
  }
  // Vanilla biome environments, restored when custom data is removed.
  private static List<BiomeEnvSetup> OriginalEnvironments = [];
  private static bool EnvironmentsApplied;
  public static void Initialize()
  {
    OriginalEnvironments = [.. EnvMan.instance.m_biomes];
    EnvironmentsApplied = false;
  }
  public static void LoadEnvironments()
  {
    if (!EnvMan.instance) return;
    if (Environments.Count > 0)
    {
      SetupBiomeEnvs(Environments);
      EnvironmentsApplied = true;
    }
    else if (EnvironmentsApplied && OriginalEnvironments.Count > 0)
    {
      SetupBiomeEnvs(OriginalEnvironments);
      EnvironmentsApplied = false;
    }
  }
  private static void SetupBiomeEnvs(List<BiomeEnvSetup> data)
  {
    var em = EnvMan.instance;
    foreach (var list in LocationList.m_allLocationLists)
      list.m_biomeEnvironments.Clear();
    em.m_biomes.Clear();
    foreach (var biome in data)
      em.AppendBiomeSetup(biome);
    em.m_environmentPeriod = -1;
    em.m_firstEnv = true;

  }
  private static void AddTranslation(Heightmap.Biome biome, string name)
  {
    var key = "biome_" + biome.ToString().ToLowerInvariant();
    var value = name == "" ? biome.ToString() : name;
    Localization.instance.m_translations[key] = value;
  }
  private static Heightmap.Biome NextBiome(Heightmap.Biome biome)
  {
    var number = (uint)biome;
    if (number == 0x80)
      throw new Exception("Too many biomes.");
    if (number == 0x80000000)
      return (Heightmap.Biome)0x80;
    return (Heightmap.Biome)(2 * number);
  }
  private static SetResult Set(Dictionary<string, string> files)
  {
    var previous = (EnvKeys, UsedGlobalKeys, BiomeData, BiomeToColor, Environments,
      LavaBiomes, FullLavaBiomes, NoBuildBiomes, BiomeForestMultiplier, NameToBiome,
      BiomeToDisplayName, BiomeToTerrain, BiomeToNature, MissingEnvironments, HasData, EnvironmentsApplied);
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(files);
    }
    finally
    {
      if (result != SetResult.Applied)
        (EnvKeys, UsedGlobalKeys, BiomeData, BiomeToColor, Environments,
          LavaBiomes, FullLavaBiomes, NoBuildBiomes, BiomeForestMultiplier, NameToBiome,
          BiomeToDisplayName, BiomeToTerrain, BiomeToNature, MissingEnvironments, HasData, EnvironmentsApplied) = previous;
    }
  }
  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    if (!Yaml.TryDeserialize<BiomeYaml>(files, out var data)) return SetResult.Failed;
    if (files.Count > 0 && data.Count == 0)
    {
      Log.Warning("Failed to load any biome data.");
      return SetResult.Failed;
    }
    Dictionary<Heightmap.Biome, string> translations = [];
    Load(data, translations);
    LoadEnvironments();
    foreach (var translation in translations)
      AddTranslation(translation.Key, translation.Value);
    Refresh.Request(Regen.Terrain | Regen.Water | Regen.Minimap);
    return SetResult.Applied;
  }
  public static void SetupWatcher()
  {
    static void callback()
    {
      if (ZNet.m_instance == null) NamesFromFile();
      else ReadConfigs();
    }
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataBiome, callback);
  }

  public static bool CheckKeys(EnvEntry env) => !EnvKeys.TryGetValue(env, out var keys) || keys.CheckKeys();
  private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();
  public static bool UsesGlobalKey(string key)
  {
    if (UsedGlobalKeys.Count == 0) return false;
    if (key == null || key == "") return false;
    return UsedGlobalKeys.Contains(NormalizeKey(key));
  }
  public static bool HasRelevantGlobalKeyChanges(HashSet<string> oldKeys, List<string> newKeys)
  {
    if (UsedGlobalKeys.Count == 0) return false;
    HashSet<string> oldRelevant = [];
    HashSet<string> newRelevant = [];
    foreach (var key in oldKeys)
    {
      if (UsesGlobalKey(key))
        oldRelevant.Add(key);
    }
    foreach (var key in newKeys)
    {
      if (UsesGlobalKey(key))
        newRelevant.Add(key);
    }
    return !oldRelevant.SetEquals(newRelevant);
  }
}

public class GenerateWorldMap
{

  static float GetBiomeHeight(Heightmap.Biome biome, float wx, float wy, out Color mask, bool preGeneration = false, bool riverPreDN = true)
  {
    var height = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out mask, preGeneration, riverPreDN);
    return height;
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(GetBiomeHeight).operand)
      .InstructionEnumeration();
  }
}
public class UpdateBiome
{
  // Last biome gets negative number that can't be translated.
  static readonly char[] EmptyChars = [];
  static char[] OriginalChars = [];
  static string AppendTerritoryName(string text, Vector3 coords)
  {
    var territory = BiomeCalculator.GetTerritory(coords.x, coords.z);
    if (territory == null || territory.name == "") return text;
    return $"{text} ({territory.name})";
  }
  internal static void PrepareTerritoryNameDisplay()
  {
    OriginalChars = Localization.instance.m_endChars;
    Localization.instance.m_endChars = EmptyChars;
  }
  internal static IEnumerable<CodeInstruction> AppendTerritoryNameToDisplay(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Stloc_2))
      .Insert(
        new CodeInstruction(OpCodes.Ldloc_0),
        new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(AppendTerritoryName).operand)
      )
      .InstructionEnumeration();
  }
  internal static void RestoreTerritoryNameDisplay()
  {
    Localization.instance.m_endChars = OriginalChars;
  }
}

public class GetAvailableEnvironments
{
  internal static List<EnvEntry> FilterEnvironmentRules(List<EnvEntry> result)
  {
    if (result == null) return null!;
    return [.. result.Where(BiomeManager.CheckKeys)];
  }
}

public class RPC_GlobalKeys
{
  internal static void ResetEnvironmentPeriod(List<string> keys)
  {
    if (!BiomeManager.HasRelevantGlobalKeyChanges(ZoneSystem.instance.m_globalKeys, keys))
      return;
    EnvMan.instance.m_environmentPeriod = 0;
  }
}

public class GlobalKeyAdd
{
  internal static void ResetEnvironmentPeriod(string keyStr)
  {
    if (!BiomeManager.UsesGlobalKey(keyStr))
      return;
    EnvMan.instance.m_environmentPeriod = 0;
  }
}
public class GlobalKeyRemove
{
  internal static void ResetEnvironmentPeriod(string keyStr, bool __result)
  {
    // Nothing removed, nothing to do.
    if (!__result) return;
    if (!BiomeManager.UsesGlobalKey(keyStr))
      return;
    EnvMan.instance.m_environmentPeriod = 0;
  }
}

// Replaces hardcode vanilla implementation with a more flexible one.
[HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.RandomBiomeFromBiomes))]
public class RandomBiomeFromBiomes
{
  static bool Prefix(Heightmap.Biome biome, ref Heightmap.Biome __result)
  {
    var flags = (uint)biome;
    if (flags == 0 || (flags & (flags - 1)) == 0)
    {
      __result = biome;
      return false;
    }

    var count = 0;
    for (var remaining = flags; remaining != 0; remaining &= remaining - 1)
      count++;

    var selected = UnityEngine.Random.Range(0, count);
    for (var remaining = flags; ; remaining &= remaining - 1)
    {
      var flag = remaining & (0u - remaining);
      if (selected-- == 0)
      {
        __result = (Heightmap.Biome)flag;
        return false;
      }
    }
  }
}

[HarmonyPatch(typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GetRandomPointByBiome))]
public class GetRandomPointByBiome
{
  public static HashSet<Heightmap.Biome> Warned = [];
  static bool Prefix(AltBiomeWorldData __instance, Heightmap.Biome biome, ref BiomePointCoordinate __result)
  {
    if (biome == Heightmap.Biome.None)
      return false;
    if (!__instance.Biomes.TryGetValue(biome, out var biomeData) || biomeData.AllPoints.Count == 0)
    {
      if (!Warned.Contains(biome))
      {
        Log.Warning($"Biome {biome} doesn't exist in the map!");
        Warned.Add(biome);
      }
      return false;
    }
    return true;
  }
}


[HarmonyPatch(typeof(BiomeHelpers), nameof(BiomeHelpers.ToBiomeIndex))]
public class ToBiomeIndex
{
  private static readonly Dictionary<Heightmap.Biome, Heightmap.BiomeIndex> BiomeToIndex = CreateBiomeToIndex();
  internal static readonly Dictionary<Heightmap.BiomeIndex, Heightmap.Biome> IndexToBiome = BiomeToIndex
    .ToDictionary(pair => pair.Value, pair => pair.Key);

  private static Dictionary<Heightmap.Biome, Heightmap.BiomeIndex> CreateBiomeToIndex()
  {
    Dictionary<Heightmap.Biome, Heightmap.BiomeIndex> result = [];
    result[Heightmap.Biome.None] = Heightmap.BiomeIndex.None;
    for (var bit = 0; bit < 32; bit++)
    {
      var index = bit switch
      {
        7 => 10,
        8 => 8,
        9 => 9,
        _ => bit + 1
      };
      result[(Heightmap.Biome)(1u << bit)] = (Heightmap.BiomeIndex)index;
    }
    return result;
  }

  static bool Prefix(Heightmap.Biome b, ref Heightmap.BiomeIndex __result)
  {
    if (BiomeToIndex.TryGetValue(b, out var index))
      __result = index;
    else
      Log.Warning($"Biome {b} does not have a corresponding index.");
    return false;
  }
}

[HarmonyPatch(typeof(BiomeHelpers), nameof(BiomeHelpers.ToBiome))]
public class ToBiome
{
  static bool Prefix(Heightmap.BiomeIndex b, ref Heightmap.Biome __result)
  {
    if (ToBiomeIndex.IndexToBiome.TryGetValue(b, out var biome))
      __result = biome;
    else
      Log.Warning($"Biome index {b} does not have a corresponding biome.");
    return false;
  }
}