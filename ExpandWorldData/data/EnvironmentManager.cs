using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData;

public class EnvironmentManager
{
  public static string FileName = "expand_environments.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_environments*.yaml";
  private static Dictionary<string, EnvSetup> Originals = [];
  public static Dictionary<string, EnvironmentData> Extra = [];
  public static EnvSetup FromData(EnvironmentYaml data, string fileName)
  {
    EnvSetup env = new() { m_psystems = [] };
    if (Originals.TryGetValue(data.particles, out var setup))
      env = setup.Clone();
    else if (Originals.TryGetValue(data.name, out setup))
      env = setup.Clone();
    else
      Log.Warning($"{fileName}: Failed to find a particle system \"{data.particles}\" for environment {data.name}. Make sure field \"particles\" is set correctly or remove this entry.");

    env.m_name = data.name;
    env.m_default = data.isDefault;
    env.m_isWet = data.isWet;
    env.m_isFreezing = data.isFreezing;
    env.m_isFreezingAtNight = data.isFreezingAtNight;
    env.m_isCold = data.isCold;
    env.m_isColdAtNight = data.isColdAtNight;
    env.m_alwaysDark = data.alwaysDark;
    env.m_ambColorNight = DataManager.Sanity(data.ambColorNight);
    env.m_ambColorDay = DataManager.Sanity(data.ambColorDay);
    env.m_fogColorNight = DataManager.Sanity(data.fogColorNight);
    env.m_fogColorMorning = DataManager.Sanity(data.fogColorMorning);
    env.m_fogColorDay = DataManager.Sanity(data.fogColorDay);
    env.m_fogColorEvening = DataManager.Sanity(data.fogColorEvening);
    env.m_fogColorSunNight = DataManager.Sanity(data.fogColorSunNight);
    env.m_fogColorSunMorning = DataManager.Sanity(data.fogColorSunMorning);
    env.m_fogColorSunDay = DataManager.Sanity(data.fogColorSunDay);
    env.m_fogColorSunEvening = DataManager.Sanity(data.fogColorSunEvening);
    env.m_fogDensityNight = data.fogDensityNight;
    env.m_fogDensityMorning = data.fogDensityMorning;
    env.m_fogDensityDay = data.fogDensityDay;
    env.m_fogDensityEvening = data.fogDensityEvening;
    env.m_sunColorNight = DataManager.Sanity(data.sunColorNight);
    env.m_sunColorMorning = DataManager.Sanity(data.sunColorMorning);
    env.m_sunColorDay = DataManager.Sanity(data.sunColorDay);
    env.m_sunColorEvening = DataManager.Sanity(data.sunColorEvening);
    env.m_lightIntensityDay = data.lightIntensityDay;
    env.m_lightIntensityNight = data.lightIntensityNight;
    env.m_sunAngle = data.sunAngle;
    env.m_windMin = data.windMin;
    env.m_windMax = data.windMax;
    env.m_rainCloudAlpha = data.rainCloudAlpha;
    env.m_ambientVol = data.ambientVol;
    env.m_ambientList = data.ambientList;
    env.m_musicMorning = data.musicMorning;
    env.m_musicEvening = data.musicEvening;
    env.m_musicDay = data.musicDay;
    env.m_musicNight = data.musicNight;

    EnvironmentData extra = new(data);
    if (extra.IsValid())
      Extra[data.name] = extra;
    return env;
  }
  public static EnvironmentYaml ToData(EnvSetup env)
  {
    EnvironmentYaml data = new()
    {
      name = env.m_name,
      isDefault = env.m_default,
      isWet = env.m_isWet,
      isFreezing = env.m_isFreezing,
      isFreezingAtNight = env.m_isFreezingAtNight,
      isCold = env.m_isCold,
      isColdAtNight = env.m_isColdAtNight,
      alwaysDark = env.m_alwaysDark,
      ambColorNight = env.m_ambColorNight,
      ambColorDay = env.m_ambColorDay,
      fogColorNight = env.m_fogColorNight,
      fogColorMorning = env.m_fogColorMorning,
      fogColorDay = env.m_fogColorDay,
      fogColorEvening = env.m_fogColorEvening,
      fogColorSunNight = env.m_fogColorSunNight,
      fogColorSunMorning = env.m_fogColorSunMorning,
      fogColorSunDay = env.m_fogColorSunDay,
      fogColorSunEvening = env.m_fogColorSunEvening,
      fogDensityNight = env.m_fogDensityNight,
      fogDensityMorning = env.m_fogDensityMorning,
      fogDensityDay = env.m_fogDensityDay,
      fogDensityEvening = env.m_fogDensityEvening,
      sunColorNight = env.m_sunColorNight,
      sunColorMorning = env.m_sunColorMorning,
      sunColorDay = env.m_sunColorDay,
      sunColorEvening = env.m_sunColorEvening,
      lightIntensityDay = env.m_lightIntensityDay,
      lightIntensityNight = env.m_lightIntensityNight,
      sunAngle = env.m_sunAngle,
      windMin = env.m_windMin,
      windMax = env.m_windMax,
      rainCloudAlpha = env.m_rainCloudAlpha,
      ambientVol = env.m_ambientVol,
      ambientList = env.m_ambientList,
      musicMorning = env.m_musicMorning,
      musicEvening = env.m_musicEvening,
      musicDay = env.m_musicDay,
      musicNight = env.m_musicNight
    };
    return data;
  }

  public static void ToFile()
  {
    if (Helper.IsClient() || !Configuration.DataEnvironments) return;
    if (File.Exists(FilePath)) return;
    Save(EnvMan.instance.m_environments, false);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    SetOriginals();
    var yaml = Configuration.DataEnvironments ? DataManager.Read<EnvironmentYaml, EnvSetup>(Pattern, FromData) : "";
    Configuration.valueEnvironmentData.Value = yaml;
    Set(yaml);
  }

  private static void SetOriginals()
  {
    var newOriginals = LocationList.m_allLocationLists
      .Select(list => list.m_environments)
      .Append(EnvMan.instance.m_environments)
      .SelectMany(list => list).ToLookup(env => env.m_name, env => env).ToDictionary(kvp => kvp.Key, kvp => kvp.First());
    // Needs to be set once per world. This can be checked detected by checking location lists.
    if (newOriginals.Count > 0) Originals = newOriginals;
  }
  public static void FromSetting(string yaml)
  {
    if (Helper.IsClient()) Set(yaml);
  }
  private static void Set(string yaml)
  {
    SetOriginals();
    Extra.Clear();
    if (yaml == "" || !Configuration.DataEnvironments) return;
    try
    {
      var data = Yaml.Deserialize<EnvironmentYaml>(yaml, "Environments")
        .Select(d => FromData(d, "Environments")).ToList();
      if (data.Count == 0)
      {
        Log.Warning($"Failed to load any environment data.");
        return;
      }
      if (Configuration.DataMigration && Helper.IsServer() && AddMissingEntries(data))
      {
        // Watcher triggers reload.
        return;
      }
      Log.Info($"Reloading environment data ({data.Count} entries).");
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
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
  }

  private static bool AddMissingEntries(List<EnvSetup> entries)
  {
    Dictionary<string, List<EnvSetup>> perFile = [];
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
    Dictionary<string, List<EnvSetup>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.m_name);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.m_name}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.BaseDirectory, $"expand_environments{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value.Select(ToData));
      File.WriteAllText(file, yaml);
    }
  }


  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, FromFile);
  }
}
