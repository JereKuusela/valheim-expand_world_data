using System.Collections.Generic;

namespace ExpandWorldData;

//<summary>External blueprint data from configs (not directly from the blueprint file).
public class BlueprintMetaData(string[] snapPieces)
{
  public string[] SnapPieces = snapPieces;
}
public class BlueprintManager
{
  public static bool Has(string name) => BlueprintFiles.ContainsKey(Parse.Name(name));
  public static bool TryGet(string name, out Blueprint bp)
  {
    if (!Has(name)) Load(name);
    return BlueprintFiles.TryGetValue(Parse.Name(name), out bp);
  }
  public static Dictionary<string, Blueprint> BlueprintFiles = [];
  private static readonly Dictionary<string, BlueprintMetaData> MetaData = [];
  public static bool Load(string name) => Load(name, []);
  public static bool Load(string name, string[] snapPieces)
  {
    var hash = name.GetStableHashCode();
    if (ZNetScene.instance.m_namedPrefabs.ContainsKey(hash)) return true;
    if (BlueprintFiles.ContainsKey(name) && MetaData.TryGetValue(name, out var data))
    {
      // Already loaded so no point to check again.
      if (data.SnapPieces == snapPieces) return true;
    }
    if (Blueprints.TryGetBluePrint(name, out var bp))
    {
      MetaData[name] = new(snapPieces);
      bp.LoadSnapPoints(snapPieces);
      bp.Center();
      BlueprintFiles[name] = bp;
      foreach (var obj in bp.Objects)
      {
        if (obj.Chance == 0) continue;
        Load(obj.Prefab, snapPieces);
      }
      return true;
    }
    EWD.Log.LogWarning($"Object / blueprint {name} not found!");
    return false;
  }
  public static void Reload(string name)
  {
    if (!Has(name)) return;
    if (!Blueprints.TryGetBluePrint(name, out _)) return;
    EWD.Log.LogInfo($"Reloading blueprint {name}.");
    BlueprintFiles.Remove(name);
    if (MetaData.TryGetValue(name, out var data))
      Load(name, data.SnapPieces);
    else
      Load(name);
  }
}