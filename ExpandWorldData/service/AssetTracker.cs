using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Service;

public class AssetTracker
{
  private static readonly Dictionary<string, string> PrefabToBundleMapping = [];
  private static readonly Dictionary<string, string> BundleToModMapping = [];

  internal static void MapPrefabsToBundles()
  {
    foreach (AssetBundle? bundle in AssetBundle.GetAllLoadedAssetBundles())
    {
      string[]? allAssetNames = bundle.GetAllAssetNames();
      IEnumerable<string>? prefabNames = allAssetNames.Where(name => name.EndsWith(".prefab"));

      foreach (string? prefab in prefabNames)
      {
        string simpleName = System.IO.Path.GetFileNameWithoutExtension(prefab);
        PrefabToBundleMapping[simpleName] = bundle.name;
      }
    }
  }

  internal static void MapBundlesToMods()
  {
    // AppDomain.CurrentDomain.GetAssemblies() didn't work here since they are dynamically loaded. This worked though.
    var allMods = Chainloader.PluginInfos.Values;

    Dictionary<string, string[]> modResources = Chainloader.PluginInfos.ToDictionary(kvp => kvp.Value.Metadata.Name, kvp =>
    {
      try
      {
        return kvp.Value.Instance.GetType().Assembly.GetManifestResourceNames();
      }
      catch
      {
        return [];
      }
    });
    foreach (string? bundleName in PrefabToBundleMapping.Values.Distinct())
    {
      var mod = modResources.FirstOrDefault(kvp => kvp.Value.Any(name => name.EndsWith(bundleName, StringComparison.OrdinalIgnoreCase)));
      if (mod.Key != null)
      {
        BundleToModMapping[bundleName] = mod.Key;
      }
    }
  }


  public static string GetModFromPrefab(string prefabName)
  {
    if (PrefabToBundleMapping.Count == 0)
    {
      MapPrefabsToBundles();
      MapBundlesToMods();
    }
    if (PrefabToBundleMapping.TryGetValue(prefabName.ToLowerInvariant(), out string? bundleName))
    {
      if (BundleToModMapping.TryGetValue(bundleName, out var str))
      {
        return str;
      }
    }

    return "Valheim";
  }
  public static string GetFileNameFromMod(string modName)
  {
    if (modName == "" || modName == "Valheim") return "";
    return $"_{modName.Replace(" ", "_").ToLowerInvariant()}";
  }
  public static string GetBundleForPrefab(string prefabName)
  {
    return PrefabToBundleMapping.TryGetValue(prefabName, out string? bundleName) ? bundleName : "";
  }
}