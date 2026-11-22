using System.Collections.Generic;
using System.IO;
using Service;
using Common;
using Data;

namespace ExpandWorldData.Data;

//<summary>External blueprint data from configs (not directly from the blueprint file).
public class BlueprintMetaData(string[] snapPieces)
{
  public string[] SnapPieces = snapPieces;
}
public class BlueprintManager
{
  public static bool Has(string name) => BlueprintCache.IsLoaded(Parse.Name(name));
  public static bool TryGet(string name, out Blueprint bp)
  {
    if (!Has(name)) Load(name);
    return BlueprintCache.TryGet(Parse.Name(name), out bp);
  }
  private static readonly Dictionary<string, BlueprintMetaData> MetaData = [];
  // Snap points must be loaded before the blueprint is centered.
  public static void ApplySnapPieces(Blueprint bp)
  {
    if (MetaData.TryGetValue(bp.Name, out var data))
      bp.LoadSnapPoints(data.SnapPieces);
  }
  public static bool Load(string name) => Load(name, []);
  public static bool Load(string name, string[] snapPieces)
  {
    var hash = name.GetStableHashCode();
    if (ZNetScene.instance.m_namedPrefabs.ContainsKey(hash)) return true;
    if (BlueprintCache.IsLoaded(name) && MetaData.TryGetValue(name, out var data))
    {
      // Already loaded so no point to check again.
      if (data.SnapPieces == snapPieces) return true;
    }
    MetaData[name] = new(snapPieces);
    BlueprintCache.Remove(name);
    if (BlueprintCache.TryGet(name, out var bp))
    {
      foreach (var obj in bp.Objects)
      {
        if (obj.Chance == 0) continue;
        Load(obj.Prefab, snapPieces);
      }
      return true;
    }
    Log.Warning($"Object / blueprint {name} not found!");
    return false;
  }
  // The watcher has already removed the blueprint from the cache.
  public static void Reload(string name, bool wasLoaded)
  {
    if (!wasLoaded) return;
    Log.Info($"Reloading blueprint {name}.");
    if (MetaData.TryGetValue(name, out var data))
      Load(name, data.SnapPieces);
    else
      Load(name);
  }
}
