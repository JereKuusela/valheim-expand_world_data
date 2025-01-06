using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;

// TODO: Biomes should be optimized. Scale them by world size on load.
namespace ExpandWorldData;
public class BiomeManager
{
  public static string FileName = "expand_biomes.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_biomes*.yaml";
  public static Dictionary<string, EnvironmentData> Extra = [];

  public static Dictionary<EnvEntry, EnvEntryKeys> EnvKeys = [];

  public static Heightmap.Biome LavaBiomes = Heightmap.Biome.AshLands;
  // Minor optimization to skip terrain color based calculations.
  public static Heightmap.Biome FullLavaBiomes = Heightmap.Biome.AshLands;
  public static Heightmap.Biome NoBuildBiomes = 0;

  public static EnvEntry FromData(BiomeEnvironment data, Dictionary<EnvEntry, EnvEntryKeys> keys)
  {
    EnvEntry env = new()
    {
      m_environment = data.environment,
      m_weight = data.weight,
      m_ashlandsOverride = data.ashlandsOverride ?? data.environment == "Ashlands_SeaStorm",
      m_deepnorthOverride = data.deepNorthOverride ?? false
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
  ///<summary>Lower case biome names for easier data loading.</summary>
  private static Dictionary<string, Heightmap.Biome> NameToBiome = OriginalBiomes.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
  ///<summary>Original biome names because some mods rely on Enum.GetName(s) returning uppercase values.</summary>
  public static Dictionary<Heightmap.Biome, string> BiomeToDisplayName = OriginalBiomes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
  private static Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToTerrain = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
  private static Dictionary<Heightmap.Biome, Heightmap.Biome> BiomeToNature = NameToBiome.ToDictionary(kvp => kvp.Value, kvp => kvp.Value);
  private static readonly Dictionary<Heightmap.Biome, Color> BiomeToColor = [];
  private static readonly Dictionary<Heightmap.Biome, BiomeData> BiomeData = [];
  public static bool TryGetColor(Heightmap.Biome biome, out Color color) => BiomeToColor.TryGetValue(biome, out color);
  public static bool TryGetData(Heightmap.Biome biome, out BiomeData data) => BiomeData.TryGetValue(biome, out data);
  public static bool TryGetBiome(string name, out Heightmap.Biome biome) => NameToBiome.TryGetValue(name.ToLowerInvariant(), out biome);
  public static Heightmap.Biome GetBiome(string name) => NameToBiome.TryGetValue(name.ToLowerInvariant(), out var biome) ? biome : Heightmap.Biome.None;
  public static bool TryGetDisplayName(Heightmap.Biome biome, out string name) => BiomeToDisplayName.TryGetValue(biome, out name);
  public static Heightmap.Biome GetTerrain(Heightmap.Biome biome) => BiomeToTerrain.TryGetValue(biome, out var terrain) ? terrain : biome;
  public static Heightmap.Biome GetNature(Heightmap.Biome biome) => BiomeToNature.TryGetValue(biome, out var nature) ? nature : biome;
  public static BiomeEnvSetup FromData(BiomeYaml data, Dictionary<EnvEntry, EnvEntryKeys> keys)
  {
    var biome = new BiomeEnvSetup
    {
      m_biome = DataManager.ToBiomes(data.biome),
      m_environments = data.environments.Select(d => FromData(d, keys)).ToList(),
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
      environments = biome.m_environments.Select(ToData).ToArray(),
      musicMorning = biome.m_musicMorning,
      musicEvening = biome.m_musicEvening,
      musicDay = biome.m_musicDay,
      musicNight = biome.m_musicNight,
      color = Heightmap.GetBiomeColor(biome.m_biome),
      mapColor = Minimap.instance.GetPixelColor(biome.m_biome),
      // Reduces the mountains on the map.
      mapColorMultiplier = biome.m_biome == Heightmap.Biome.AshLands ? 0.5f : 1f,
      lava = biome.m_biome == Heightmap.Biome.AshLands ? "true" : ""
    };
  }

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataBiome) return;
    if (File.Exists(FilePath)) return;
    var biomes = OriginalBiomes.Values.Select(b => EnvMan.instance.m_biomes.Find(ev => ev.m_biome == b)).Where(b => b != null);
    var yaml = Yaml.Serializer().Serialize(biomes.Select(ToData).ToList());
    File.WriteAllText(FilePath, yaml);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    var yaml = Configuration.DataBiome ? DataManager.Read<BiomeYaml>(Pattern) : "";
    Configuration.valueBiomeData.Value = yaml;
    Set(yaml);
  }
  public static void NamesFromFile()
  {
    if (!Configuration.DataBiome) return;
    LoadNames(DataManager.ReadData<BiomeYaml>(Pattern));
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  public static bool BiomeForestMultiplier = false;

  private static List<BiomeYaml> Parse(string yaml)
  {
    List<BiomeYaml> rawData = [];
    if (Configuration.DataBiome)
    {
      try
      {
        rawData = Yaml.Deserialize<BiomeYaml>(yaml, "Biomes");
      }
      catch (Exception e)
      {
        Log.Warning($"Failed to load any biome data.");
        Log.Error(e.Message);
        Log.Error(e.StackTrace);
      }
    }
    return rawData;
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
  private static void Load(string yaml)
  {
    if (yaml == "" || !Configuration.DataBiome) return;
    var rawData = Parse(yaml);
    if (rawData.Count > 0)
      Log.Info($"Reloading biome data ({rawData.Count} entries).");
    EnvKeys.Clear();
    BiomeData.Clear();
    BiomeToColor.Clear();
    LavaBiomes = 0;
    FullLavaBiomes = 0;
    NoBuildBiomes = 0;
    NameToBiome = OriginalBiomes.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
    BiomeToDisplayName = OriginalBiomes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    var lastBiome = Heightmap.Biome.Mistlands;

    foreach (var item in rawData)
    {
      var biome = lastBiome;
      if (NameToBiome.TryGetValue(item.biome.ToLowerInvariant(), out var defaultBiome))
      {
        biome = defaultBiome;
        if (item.name != "")
          AddTranslation(biome, item.name);
      }
      else
      {
        biome = lastBiome = NextBiome(lastBiome);
        NameToBiome.Add(item.biome.ToLowerInvariant(), biome);
        BiomeToDisplayName[biome] = item.biome;
        AddTranslation(biome, item.name);
      }
      DataManager.Sanity(ref item.mapColor);
      DataManager.Sanity(ref item.color);
      BiomeData extra = new(item);
      if (extra.lava)
        LavaBiomes |= biome;
      if (extra.lava && item.color.a == 1 && item.color.r == 1)
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
    Environments = rawData.Select(d => FromData(d, EnvKeys)).ToList();
    // This tracks if content (environments) have been loaded.
    if (ZoneSystem.instance.m_locationsByHash.Count > 0)
      LoadEnvironments();
    EWD.Instance.InvokeRegenerate();
  }
  public static void LoadEnvironments()
  {
    if (!Configuration.DataBiome || Environments.Count == 0) return;
    SetupBiomeEnvs(Environments);
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
  private static void Set(string yaml)
  {
    Load(yaml);
  }
  public static void SetupWatcher()
  {
    static void callback()
    {
      if (ZNet.m_instance == null) NamesFromFile();
      else FromFile();
    }
    Yaml.SetupWatcher(Pattern, callback);
  }

  // These must be stored in static fields to avoid garbage collection.
  static readonly float[] biomeWeights = new float[33];
  static readonly Heightmap.Biome[] indexToBiome = biomeWeights.Select((_, i) => (Heightmap.Biome)(i < 2 ? i : 2 << (i - 2))).ToArray();
  // dotnet caches/inlines access to static readonly fields.
  // So the readonly arrays must be resized in advance.
  public static void SetupBiomeArrays()
  {
#pragma warning disable CS8500
    unsafe
    {
      fixed (void* ptr = &Heightmap.s_indexToBiome)
        *(object*)ptr = indexToBiome;
      fixed (void* ptr = &Heightmap.s_tempBiomeWeights)
        *(object*)ptr = biomeWeights;
    }
#pragma warning restore CS8500
    // Dictionary can be updated in the place.
    for (int i = 0; i < indexToBiome.Length; ++i)
      Heightmap.s_biomeToIndex[indexToBiome[i]] = i;
  }

  public static bool CheckKeys(EnvEntry env) => !EnvKeys.TryGetValue(env, out var keys) || keys.CheckKeys();
}

//[HarmonyPatch(typeof(Minimap), nameof(Minimap.GenerateWorldMap))]
public class GenerateWorldMap
{

  static float GetBiomeHeight(Heightmap.Biome biome, float wx, float wy, out Color mask, bool preGeneration = false)
  {
    var height = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out mask, preGeneration);
    return height;
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Field(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(GetBiomeHeight).operand)
      .InstructionEnumeration();
  }
}
[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateBiome))]
public class UpdateBiome
{
  // Last biome gets negative number that can't be translated.
  static readonly char[] EmptyChars = [];
  static char[] OriginalChars = [];
  static void Prefix()
  {
    OriginalChars = Localization.instance.m_endChars;
    Localization.instance.m_endChars = EmptyChars;
  }
  static void Postfix()
  {
    Localization.instance.m_endChars = OriginalChars;
  }
}

[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.GetAvailableEnvironments))]
public class GetAvailableEnvironments
{
  static void Postfix(List<EnvEntry> __result)
  {
    if (__result == null) return;
    if (BiomeManager.EnvKeys.Count == 0) return;
    __result = __result.Where(BiomeManager.CheckKeys).ToList();
  }
}


[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_GlobalKeys))]
public class RPC_GlobalKeys
{
  static void Postfix()
  {
    if (BiomeManager.EnvKeys.Count > 0)
      EnvMan.instance.m_environmentPeriod = 0;
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_SetGlobalKey))]
public class RPC_SetGlobalKey
{
  static void Postfix()
  {
    if (BiomeManager.EnvKeys.Count > 0)
      EnvMan.instance.m_environmentPeriod = 0;
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_RemoveGlobalKey))]
public class RPC_RemoveGlobalKey
{
  static void Postfix()
  {
    if (BiomeManager.EnvKeys.Count > 0)
      EnvMan.instance.m_environmentPeriod = 0;
  }
}