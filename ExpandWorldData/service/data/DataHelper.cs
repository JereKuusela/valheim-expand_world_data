using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;

namespace Data;

public class DataHelper
{
  public static DataEntry? Merge(params DataEntry?[] datas)
  {
    var nonNull = datas.Where(d => d != null).ToArray();
    if (nonNull.Length == 0) return null;
    if (nonNull.Length == 1) return nonNull[0];
    DataEntry result = new();
    foreach (var data in nonNull)
      result.Load(data!);
    return result;
  }
  public static bool Exists(int hash) => DataLoading.Data.ContainsKey(hash);
  public static bool Match(int hash, ZDO zdo)
  {
    if (DataLoading.Data.TryGetValue(hash, out var data))
    {
      return data.Match([], zdo);
    }
    return false;
  }
  public static bool Match(int hash, ZDO zdo, Dictionary<string, string> pars)
  {
    if (DataLoading.Data.TryGetValue(hash, out var data))
    {
      return data.Match(pars, zdo);
    }
    return false;
  }
  public static DataEntry? Get(string name) => name == "" ? null : DataLoading.Get(name);
  public static void Init(GameObject obj, Vector3 pos, Quaternion rot, Vector3? scale, DataEntry? data, Dictionary<string, string> pars)
  {
    if (data == null && scale == null) return;
    if (!obj.TryGetComponent<ZNetView>(out var view)) return;
    var prefab = Utils.GetPrefabName(obj).GetStableHashCode();
    ZNetView.m_initZDO = ZDOMan.instance.CreateNewZDO(pos, prefab);
    data?.Write(pars, ZNetView.m_initZDO);
    ZNetView.m_initZDO.m_rotation = rot.eulerAngles;
    ZNetView.m_initZDO.Type = view.m_type;
    ZNetView.m_initZDO.Distant = view.m_distant;
    ZNetView.m_initZDO.Persistent = view.m_persistent;
    ZNetView.m_initZDO.m_prefab = prefab;
    if (view.m_syncInitialScale && scale != null)
      ZNetView.m_initZDO.Set(ZDOVars.s_scaleHash, scale.Value);
    ZNetView.m_initZDO.DataRevision = 0;
    // This is needed to trigger the ZDO sync.
    ZNetView.m_initZDO.IncreaseDataRevision();
  }
  public static ZDO? Init(GameObject obj, Vector3 pos, Quaternion rot, Vector3? scale, DataEntry? data)
  {
    if (data == null && scale == null) return null;
    if (!obj.TryGetComponent<ZNetView>(out var view)) return null;
    var prefab = Utils.GetPrefabName(obj).GetStableHashCode();
    ZNetView.m_initZDO = ZDOMan.instance.CreateNewZDO(pos, prefab);
    data?.Write([], ZNetView.m_initZDO);
    ZNetView.m_initZDO.m_rotation = rot.eulerAngles;
    ZNetView.m_initZDO.Type = view.m_type;
    ZNetView.m_initZDO.Distant = view.m_distant;
    ZNetView.m_initZDO.Persistent = view.m_persistent;
    ZNetView.m_initZDO.m_prefab = prefab;
    if (view.m_syncInitialScale && scale != null)
      ZNetView.m_initZDO.Set(ZDOVars.s_scaleHash, scale.Value);
    ZNetView.m_initZDO.DataRevision = 0;
    // This is needed to trigger the ZDO sync.
    ZNetView.m_initZDO.IncreaseDataRevision();
    return ZNetView.m_initZDO;
  }
  public static void CleanUp()
  {
    ZNetView.m_initZDO = null;
  }

  public static List<string>? GetValuesFromGroup(string group)
  {
    var hash = group.ToLowerInvariant().GetStableHashCode();
    if (DataLoading.ValueGroups.TryGetValue(hash, out var values))
      return values;
    return null;
  }
  // This is mainly used to simplify code.
  // Not very efficient because usually only a single prefab is used.
  // So only use when the result is cached.
  public static List<string> ResolvePrefabs(string values)
  {
    HashSet<string> prefabs = [];
    ResolvePrefabsSub(prefabs, values);
    return [.. prefabs];
  }
  private static void ResolvePrefabsSub(HashSet<string> prefabs, string value)
  {
    if (value == "all" || ZNetScene.instance.m_namedPrefabs.ContainsKey(value.GetStableHashCode()))
    {
      prefabs.Add(value);
      return;
    }
    var values = GetValuesFromGroup(value);
    if (values != null)
    {
      foreach (var v in values)
        ResolvePrefabsSub(prefabs, v);
      return;
    }
    Log.Warning($"Failed to resolve prefab: {value}");
  }
}