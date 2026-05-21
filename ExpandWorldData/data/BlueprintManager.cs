// 0.0.1, May 18, 2026
using System.Collections.Generic;
using System.IO;
using Service;

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

    public static bool Load(string name) => Load(name, [], "");

    public static bool Load(string name, string[] snapPieces, string sourceBlueprint = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            string sourceInfo = string.IsNullOrEmpty(sourceBlueprint) ? "" : $" (Referenced in blueprint '{sourceBlueprint}')";
            Log.Warning($"Attempted to load a blueprint or object with an empty or whitespace name '{name}'. Check your YAML files or parent blueprints for malformed entries.{sourceInfo}");
            return false;
        }

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
                Load(obj.Prefab, snapPieces, bp.Name);
            }
            return true;
        }

        string sourceText = string.IsNullOrEmpty(sourceBlueprint) ? "" : $" (Referenced in blueprint '{sourceBlueprint}')";
        Log.Warning($"Object / blueprint '{name}' not found!{sourceText}");
        return false;
    }

    public static void Reload(string name)
    {
        if (!Has(name)) return;
        if (!Blueprints.TryGetBluePrint(name, out _)) return;
        Log.Info($"Reloading blueprint {name}.");
        BlueprintFiles.Remove(name);
        if (MetaData.TryGetValue(name, out var data))
            Load(name, data.SnapPieces);
        else
            Load(name);
    }


    private static void ReloadBlueprint(string name)
    {
        Reload(Path.GetFileNameWithoutExtension(name));
    }
    public static void SetupBlueprintWatcher()
    {
        if (!Directory.Exists(Configuration.BlueprintGlobalFolder))
            Directory.CreateDirectory(Configuration.BlueprintGlobalFolder);
        if (!Directory.Exists(Configuration.BlueprintLocalFolder))
            Directory.CreateDirectory(Configuration.BlueprintLocalFolder);
        Yaml.SetupWatcher(Configuration.BlueprintGlobalFolder, "*.blueprint", ReloadBlueprint);
        Yaml.SetupWatcher(Configuration.BlueprintGlobalFolder, "*.vbuild", ReloadBlueprint);
        if (Path.GetFullPath(Configuration.BlueprintLocalFolder) != Path.GetFullPath(Configuration.BlueprintGlobalFolder))
        {
            Yaml.SetupWatcher(Configuration.BlueprintLocalFolder, "*.blueprint", ReloadBlueprint);
            Yaml.SetupWatcher(Configuration.BlueprintLocalFolder, "*.vbuild", ReloadBlueprint);
        }
    }
}