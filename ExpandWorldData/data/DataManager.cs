
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Service;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ExpandWorldData;


[HarmonyPatch(typeof(DungeonDB), nameof(DungeonDB.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeRooms
{
  static void Postfix()
  {
    RoomLoading.Initialize();
    // Dungeons require room names to be loaded.
    Dungeon.Loader.Initialize();
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup)), HarmonyPriority(Priority.VeryLow)]
public class InitializeWorld
{
  // River generation requires biome and world data being loaded.
  // Saving is done later because that requires environments.
  static void Postfix()
  {
    // Only called for server so no need to check.
    BiomeManager.FromFile();
    WorldManager.FromFile();
  }
}


[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeContent
{
  static void Postfix()
  {
    if (Helper.IsServer())
    {
      DataLoading.Initialize();
      EnvironmentManager.ToFile();
      ClutterManager.ToFile();

      EnvironmentManager.FromFile();
      BiomeManager.LoadEnvironments();
      BiomeManager.ToFile();
      WorldManager.ToFile();

      // These are here to not have to clear location lists (slightly better compatibility).
      VegetationLoading.Initialize();
      // Clutter must be here because since SetupLocations adds prefabs to the list.
      ClutterManager.FromFile();

      // Dungeon and room data is handled elsewhere.
    }
    ZoneSystem.instance.m_locations = LocationSetup.CleanUpLocations(ZoneSystem.instance.m_locations);
    LocationSetup.SetupExtraLocations(ZoneSystem.instance.m_locations);
    LocationLoading.Initialize();
  }
}

[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawning))]
public class Spawn_WaitForConfigSync
{
  static bool Prefix() => DataManager.IsReady;
}
public class DataManager : MonoBehaviour
{
  public static bool IsReady => EWD.ConfigSync.IsSourceOfTruth || EWD.ConfigSync.InitialSyncDone;

  public static void SetupWatcher(ConfigFile config)
  {
    FileSystemWatcher watcher = new(Path.GetDirectoryName(config.ConfigFilePath), Path.GetFileName(config.ConfigFilePath));
    watcher.Changed += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.Created += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.Renamed += (s, e) => ReadConfigValues(e.FullPath, config);
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  private static void ReadConfigValues(string path, ConfigFile config)
  {
    if (!File.Exists(path)) return;
    BackupFile(path);
    try
    {
      config.Reload();
    }
    catch
    {
      EWD.Log.LogError($"There was an issue loading your {config.ConfigFilePath}");
      EWD.Log.LogError("Please check your config entries for spelling and format!");
    }
  }
  private static void SetupWatcher(string folder, string pattern, Action<string> action)
  {
    FileSystemWatcher watcher = new(folder, pattern);
    watcher.Created += (s, e) => action(e.FullPath);
    watcher.Changed += (s, e) => action(e.FullPath);
    watcher.Renamed += (s, e) => action(e.FullPath);
    watcher.Deleted += (s, e) => action(e.FullPath);
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  private static void ReloadBlueprint(string name)
  {
    BlueprintManager.Reload(Path.GetFileNameWithoutExtension(name));
  }
  public static void SetupBlueprintWatcher()
  {
    if (!Directory.Exists(Configuration.BlueprintGlobalFolder))
      Directory.CreateDirectory(Configuration.BlueprintGlobalFolder);
    if (!Directory.Exists(Configuration.BlueprintLocalFolder))
      Directory.CreateDirectory(Configuration.BlueprintLocalFolder);
    SetupWatcher(Configuration.BlueprintGlobalFolder, "*.blueprint", ReloadBlueprint);
    SetupWatcher(Configuration.BlueprintGlobalFolder, "*.vbuild", ReloadBlueprint);
    if (Path.GetFullPath(Configuration.BlueprintLocalFolder) != Path.GetFullPath(Configuration.BlueprintGlobalFolder))
    {
      SetupWatcher(Configuration.BlueprintLocalFolder, "*.blueprint", ReloadBlueprint);
      SetupWatcher(Configuration.BlueprintLocalFolder, "*.vbuild", ReloadBlueprint);
    }
  }
  public static void SetupWatcher(string pattern, Action action) => SetupWatcher(EWD.YamlDirectory, pattern, file =>
  {
    BackupFile(file);
    action();
  });
  private static void BackupFile(string path)
  {
    if (!File.Exists(path)) return;
    if (!Directory.Exists(EWD.BackupDirectory))
      Directory.CreateDirectory(EWD.BackupDirectory);
    var stamp = DateTime.Now.ToString("yyyy-MM-dd");
    var name = $"{Path.GetFileNameWithoutExtension(path)}_{stamp}{Path.GetExtension(path)}.bak";
    File.Copy(path, Path.Combine(EWD.BackupDirectory, name), true);
  }
  public static IDeserializer Deserializer() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
    .WithTypeConverter(new FloatConverter()).Build();
  public static IDeserializer DeserializerUnSafe() => new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
  .WithTypeConverter(new FloatConverter()).IgnoreUnmatchedProperties().Build();
  public static ISerializer Serializer() => new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).DisableAliases()
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).WithTypeConverter(new FloatConverter())
      .WithAttributeOverride<Color>(c => c.gamma, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.grayscale, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.linear, new YamlIgnoreAttribute())
      .WithAttributeOverride<Color>(c => c.maxColorComponent, new YamlIgnoreAttribute())
      .Build();

  public static List<T> Deserialize<T>(string raw, string fileName)
  {
    try
    {
      return Deserializer().Deserialize<List<T>>(raw) ?? [];
    }
    catch (Exception ex1)
    {
      EWD.Log.LogError($"{fileName}: {ex1.Message}");
      try
      {
        return DeserializerUnSafe().Deserialize<List<T>>(raw) ?? [];
      }
      catch (Exception)
      {
        return [];
      }
    }
  }
  private static readonly Heightmap.Biome DefaultMax =
    Heightmap.Biome.AshLands | Heightmap.Biome.BlackForest | Heightmap.Biome.DeepNorth |
    Heightmap.Biome.Meadows | Heightmap.Biome.Mistlands | Heightmap.Biome.Mountain |
    Heightmap.Biome.Ocean | Heightmap.Biome.Plains | Heightmap.Biome.Swamp;

  public static string FromList(IEnumerable<string> array) => string.Join(", ", array);
  public static string FromList(IEnumerable<ItemDrop> array) => FromList(array.Select(i => FindItemName(i.m_itemData.m_shared?.m_name ?? "")));
  public static List<string> ToList(string str, bool removeEmpty = true) => Parse.Split(str, removeEmpty).ToList();
  private static readonly Dictionary<string, int> ItemDropCache = [];
  private static ItemDrop FindItem(string name)
  {
    if (ItemDropCache.TryGetValue(name, out var value)) return ObjectDB.instance.GetItemPrefab(value).GetComponent<ItemDrop>();
    var item = ObjectDB.instance.m_items.FirstOrDefault(i => i.GetComponent<ItemDrop>()?.m_itemData.m_shared?.m_name == name);
    if (item) ItemDropCache[name] = item.name.GetStableHashCode();
    return item?.GetComponent<ItemDrop>()!;
  }
  private static string FindItemName(string name) => FindItem(name)?.name ?? "";
  public static List<ItemDrop> ToItemList(string str, bool removeEmpty = true) => Parse.Split(str, removeEmpty).Select(s => ObjectDB.instance.GetItemPrefab(s)?.GetComponent<ItemDrop>()!).Where(s => s != null).ToList();
  public static Dictionary<string, string> ToDict(string str) => ToList(str).Select(s => s.Split('=')).Where(s => s.Length == 2).ToDictionary(s => s[0].Trim(), s => s[1].Trim());
  public static string FromBiomes(Heightmap.Biome biome)
  {
    // Unused biome.
    biome &= (Heightmap.Biome)~128;
    if (biome == DefaultMax) return "";
    if (biome == Heightmap.Biome.None) return "None";
    List<string> biomes = [];
    var number = 1;
    var biomeNumber = (int)biome;
    while (number <= biomeNumber)
    {
      if ((number & biomeNumber) > 0)
      {
        if (BiomeManager.TryGetDisplayName((Heightmap.Biome)number, out var name))
          biomes.Add(name);
        else
          biomes.Add(number.ToString());
      }
      number *= 2;
    }
    return string.Join(", ", biomes);
  }
  public static string FromBiomeAreas(Heightmap.BiomeArea biomeArea)
  {
    var edge = (biomeArea & Heightmap.BiomeArea.Edge) > 0;
    var median = (biomeArea & Heightmap.BiomeArea.Median) > 0;
    if (edge && median) return "";
    if (edge) return "edge";
    if (median) return "median";
    return "";
  }
  public static string FromEnum<T>(T value) where T : struct, Enum
  {
    List<string> names = [];
    var number = 1;
    var maxNumber = (int)(object)value;
    while (number <= maxNumber)
    {
      if ((number & maxNumber) > 0)
      {
        names.Add(((T)(object)(number)).ToString());
      }
      number *= 2;
    }
    return FromList(names);
  }
  public static T ToEnum<T>(string str) where T : struct, Enum => ToEnum<T>(ToList(str));
  public static T ToEnum<T>(List<string> list) where T : struct, Enum
  {

    int value = 0;
    foreach (var item in list)
    {
      var trimmed = item.Trim();
      if (Enum.TryParse<T>(trimmed, true, out var parsed))
        value += (int)(object)parsed;
      else
        EWD.Log.LogWarning($"Failed to parse value {trimmed} as {nameof(T)}.");
    }
    return (T)(object)value;
  }
  public static T ToByteEnum<T>(List<string> list) where T : struct, Enum
  {

    byte value = 0;
    foreach (var item in list)
    {
      var trimmed = item.Trim();
      if (Enum.TryParse<T>(trimmed, true, out var parsed))
        value += (byte)(object)parsed;
      else
        EWD.Log.LogWarning($"Failed to parse value {trimmed} as {nameof(T)}.");
    }
    return (T)(object)value;
  }
  public static Heightmap.Biome ToBiomes(string biomeStr)
  {
    Heightmap.Biome result = 0;
    if (biomeStr == "")
    {
      foreach (var biome in BiomeManager.BiomeToDisplayName.Keys)
        result |= biome;
    }
    else
    {
      var biomes = Parse.Split(biomeStr);
      foreach (var biome in biomes)
      {
        if (BiomeManager.TryGetBiome(biome, out var number))
          result |= number;
        else
        {
          if (int.TryParse(biome, out var value)) result += value;
          else throw new InvalidOperationException($"Invalid biome {biome}.");
        }
      }
    }
    return result;
  }
  public static Heightmap.BiomeArea ToBiomeAreas(string m_biome)
  {
    if (m_biome == "") return Heightmap.BiomeArea.Edge | Heightmap.BiomeArea.Median;
    var biomes = Parse.Split(m_biome);
    var biomeAreas = biomes.Select(s => Enum.TryParse<Heightmap.BiomeArea>(s, true, out var area) ? area : 0);
    Heightmap.BiomeArea result = 0;
    foreach (var biome in biomeAreas) result |= biome;
    return result;
  }
  public static GameObject? ToPrefab(string str)
  {
    if (ZNetScene.instance.m_namedPrefabs.TryGetValue(str.GetStableHashCode(), out var obj))
      return obj;
    else
      EWD.Log.LogWarning($"Prefab {str} not found!");
    return null;
  }

  public static GameObject Instantiate(GameObject prefab, Vector3 pos, Quaternion rot, ZDOData? data)
  {
    var zdo = DataHelper.InitZDO(pos, rot, null, data, prefab);
    zdo?.RemoveLong(ZDOVars.s_creator);
    var obj = Instantiate(prefab, pos, rot);
    CleanGhostInit(obj);
    return obj;
  }

  public static void CleanGhostInit(GameObject obj)
  {
    if (ZNetView.m_ghostInit) CleanGhostInit(obj.GetComponent<ZNetView>());
  }
  public static void CleanGhostInit(ZNetView view)
  {
    if (ZNetView.m_ghostInit && view)
    {
      view.m_ghost = true;
      view.GetZDO().Created = false;
      ZNetScene.instance.m_instances.Remove(view.GetZDO());
    }
  }
  public static string Read(string pattern)
  {
    if (!Directory.Exists(EWD.YamlDirectory))
      Directory.CreateDirectory(EWD.YamlDirectory);
    var data = Directory.GetFiles(EWD.YamlDirectory, pattern, SearchOption.AllDirectories).Reverse().Select(name =>
      string.Join("\n", File.ReadAllLines(name).ToList())
    );
    return string.Join("\n", data) ?? "";
  }
  public static void Sanity(ref Color color)
  {
    if (color.r > 1.0f) color.r /= 255f;
    if (color.g > 1.0f) color.g /= 255f;
    if (color.b > 1.0f) color.b /= 255f;
    if (color.a > 1.0f) color.a /= 255f;
  }
  public static Color Sanity(Color color)
  {
    if (color.r > 1.0f) color.r /= 255f;
    if (color.g > 1.0f) color.g /= 255f;
    if (color.b > 1.0f) color.b /= 255f;
    if (color.a > 1.0f) color.a /= 255f;
    return color;
  }

}
#nullable disable
public class FloatConverter : IYamlTypeConverter
{
  public bool Accepts(Type type) => type == typeof(float);

  public object ReadYaml(IParser parser, Type type)
  {
    var scalar = (YamlDotNet.Core.Events.Scalar)parser.Current;
    var number = float.Parse(scalar.Value, NumberStyles.Float, CultureInfo.InvariantCulture);
    parser.MoveNext();
    return number;
  }

  public void WriteYaml(IEmitter emitter, object value, Type type)
  {
    var number = (float)value;
    emitter.Emit(new YamlDotNet.Core.Events.Scalar(number.ToString(NumberFormatInfo.InvariantInfo)));
  }
}
#nullable enable
