using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Compatibility;
using ExpandWorldData.Data;
using Service;

namespace ExpandWorldData.Vegetation;

public partial class VegetationManager
{
  private static readonly string FileName = "expand_vegetation.yaml";
  private static readonly string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static readonly string Pattern = "expand_vegetation*.yaml";
  public static readonly int HashDrop = "ews_drops".GetStableHashCode();
  private static readonly List<VegetationYaml> ExtraVegetationYamls = [];

  public static void AddVegetation(VegetationYaml yaml)
  {
    ExtraVegetationYamls.Add(yaml);
  }


  // Default items are stored to track missing entries.
  internal static List<ZoneSystem.ZoneVegetation> DefaultEntries = [];
  public static bool Customized { get; private set; }
  public static void Initialize()
  {
    DefaultEntries.Clear();
    Customized = false;
    if (Helper.IsServer())
      SetDefaultEntries();
  }

  private class Sync : YamlDataManager
  {
    protected override string FilePath => VegetationManager.FilePath;
    protected override string Pattern => VegetationManager.Pattern;
    protected override bool DataEnabled => Configuration.DataVegetation;
    protected override SetResult Set(Dictionary<string, string> files) => VegetationManager.Set(files);
    protected override void WriteDefaultConfig() => ToFile();
  }
  private static readonly Sync Instance = new();

  public static void CreateConfigs() => Instance.CreateConfigs();

  public static void ReadConfigs() => Instance.ReadConfigs();


  public static void CleanUp()
  {
    VegetationSpawning.Extra.Clear();
    VegetationSpawning.Prefabs.Clear();
  }

  private static SetResult Set(Dictionary<string, string> files)
  {
    var previousExtra = VegetationSpawning.Extra;
    var previousPrefabs = VegetationSpawning.Prefabs;
    VegetationSpawning.Extra = [];
    VegetationSpawning.Prefabs = [];
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(files);
    }
    finally
    {
      if (result != SetResult.Applied)
      {
        VegetationSpawning.Extra = previousExtra;
        VegetationSpawning.Prefabs = previousPrefabs;
      }
    }
  }
  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    var result = Load(files, out var data);
    if (result != SetResult.Applied) return result;
    Customized = !ReferenceEquals(data, DefaultEntries);
    ZoneSystem.instance.m_vegetation = data;
    IdManager.SendVegetationIds();
    return SetResult.Applied;
  }
  private static SetResult Load(Dictionary<string, string> files, out List<ZoneSystem.ZoneVegetation> data)
  {
    data = DefaultEntries;
    if (files.Count == 0)
    {
      Log.Info($"Reloading default vegetation data ({DefaultEntries.Count} entries).");
      return SetResult.Applied;
    }
    if (!Yaml.TryDeserialize<VegetationYaml, ZoneSystem.ZoneVegetation>(files, FromData, out var items)) return SetResult.Failed;
    var parsed = items.Where(veg => veg.m_prefab).ToList();
    if (parsed.Count == 0)
    {
      Log.Warning($"Failed to load any vegetation data.");
      return SetResult.Failed;
    }
    if (YamlDataManager.BackfillEnabled && AddMissingEntries(parsed))
      return SetResult.Rewritten;
    Log.Info($"Reloading vegetation data ({parsed.Count} entries).");
    data = parsed;
    return SetResult.Applied;
  }
  private static void ToFile()
  {
    var data = DefaultEntries.Select(ToData).ToList();
    if (ExtraVegetationYamls.Count > 0)
      data.AddRange(ExtraVegetationYamls);
    Save(data, false);
  }
  ///<summary>Cleans up default vegetation data and stores it to track missing entries.</summary>
  private static void SetDefaultEntries()
  {
    ZoneSystem.instance.m_vegetation = ZoneSystem.instance.m_vegetation
      .Where(veg => veg.m_prefab)
      .Where(veg => ZNetScene.instance.m_namedPrefabs.ContainsKey(veg.m_prefab.name.GetStableHashCode()))
      .Where(veg => veg.m_enable && veg.m_max > 0f).ToList();
    DefaultEntries = ZoneSystem.instance.m_vegetation;
  }

  ///<summary>Detects missing entries and adds them back to the main yaml file. Returns true if anything was added.</summary>
  // Note: This is needed people add new content mods and then complain that Expand World doesn't spawn them.
  private static bool AddMissingEntries(List<ZoneSystem.ZoneVegetation> entries)
  {
    var presentKeys = entries.Select(entry => entry.m_name).ToHashSet();
    // Some mods override prefabs so the m_prefab.name is not reliable.
    foreach (var entry in entries)
    {
      if (VegetationSpawning.Prefabs.TryGetValue(entry, out var prefabs))
        presentKeys.UnionWith(prefabs.Select(prefab => prefab.name));
    }
    // But don't use m_name because it can be anything for original items.
    var missing = YamlFiles.GetMissingEntries(DefaultEntries, presentKeys, entry => entry.m_prefab.name).Select(ToData).ToList();
    if (missing.Count == 0) return false;
    Log.Warning($"Adding {missing.Count} missing vegetation to the expand_vegetation.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<VegetationYaml> data, bool log)
  {
    YamlFiles.SavePerMod("vegetation", data, item => item.prefab, log);
  }

  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataVegetation, ReadConfigs);
  }
}
