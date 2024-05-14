
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
using Data;
using SoftReferenceableAssets;

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
    AddEmptyAssetReference();
    if (Helper.IsServer())
    {
      DataLoading.LoadEntries();
      EnvironmentManager.ToFile();

      EnvironmentManager.FromFile();
      BiomeManager.LoadEnvironments();
      BiomeManager.ToFile();
      WorldManager.ToFile();

      // These are here to not have to clear location lists (slightly better compatibility).
      VegetationLoading.Initialize();
      // Clutter must be here because since SetupLocations adds prefabs to the list.
      ClutterManager.Initialize();

      // Dungeon and room data is handled elsewhere.
    }
    ZoneSystem.instance.m_locations = ZoneSystem.instance.m_locations.Where(loc => loc.m_prefab.IsValid).ToList();
    LocationLoading.Initialize();
  }

  // Blueprints will use empty asset, which must be added to prevent errors.
  // Alternatively could make assets from blueprints but this would be quite a bit of work.
  private static void AddEmptyAssetReference()
  {
    var bundleLoader = AssetBundleLoader.Instance;
    bundleLoader.m_bundleNameToLoaderIndex[""] = 0; // So that AssetLoader ctor doesn't crash
    AssetID id = new();
    if (bundleLoader.m_assetIDToLoaderIndex.ContainsKey(id))
      return;

    AssetLoader loader = new(id, new("", ""))
    {
      m_asset = new GameObject(),
      m_referenceCounter = new(),
      m_shouldBeLoaded = true,
    };

    var index = bundleLoader.m_assetIDToLoaderIndex.Count;
    if (index >= bundleLoader.m_assetLoaders.Length)
      Array.Resize(ref bundleLoader.m_assetLoaders, bundleLoader.m_assetIDToLoaderIndex.Count + 1);
    bundleLoader.m_assetLoaders[index] = loader;
    bundleLoader.m_assetIDToLoaderIndex[id] = index;
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
        Log.Warning($"Failed to parse value {trimmed} as {parsed.GetType().Name}.");
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
        Log.Warning($"Failed to parse value {trimmed} as {nameof(T)}.");
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
      Log.Warning($"Prefab {str} not found!");
    return null;
  }

  public static GameObject Instantiate(GameObject prefab, Vector3 pos, Quaternion rot, DataEntry? data)
  {
    var zdo = DataHelper.Init(prefab, pos, rot, null, data);
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
    if (!Directory.Exists(Yaml.Directory))
      Directory.CreateDirectory(Yaml.Directory);
    var data = Directory.GetFiles(Yaml.Directory, pattern, SearchOption.AllDirectories).Reverse().Select(name =>
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