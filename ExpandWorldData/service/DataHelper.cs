using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Service;

public class DataHelper
{
  public static ZDO? InitZDO(Vector3 pos, Quaternion rot, Vector3? scale, ZDOData? data, GameObject obj)
  {
    if (!obj.TryGetComponent<ZNetView>(out var view)) return null;
    return InitZDO(pos, rot, scale, data, view);
  }

  public static ZDO? InitZDO(Vector3 pos, Quaternion rot, Vector3? scale, ZDOData? data, ZNetView view)
  {
    // No override needed.
    if (data == null && scale == null) return null;
    var prefab = view.GetPrefabName().GetStableHashCode();
    ZNetView.m_initZDO = ZDOMan.instance.CreateNewZDO(pos, prefab);
    data?.Write(ZNetView.m_initZDO);
    if (scale.HasValue)
      ZNetView.m_initZDO.Set(ZDOVars.s_scaleHash, scale.Value);
    ZNetView.m_initZDO.m_rotation = rot.eulerAngles;
    ZNetView.m_initZDO.Type = view.m_type;
    ZNetView.m_initZDO.Distant = view.m_distant;
    ZNetView.m_initZDO.Persistent = view.m_persistent;
    ZNetView.m_initZDO.m_prefab = prefab;
    ZNetView.m_initZDO.DataRevision = 1;
    return ZNetView.m_initZDO;
  }
}

public class ZDOData
{
  public static Dictionary<int, ZDOData> Cache = new();
  public static ZDOData? Create(string data)
  {
    if (data == "") return null;
    var hash = data.GetStableHashCode();
    if (!Cache.ContainsKey(hash))
      Cache[hash] = new ZDOData(data);
    return Cache[hash];
  }
  public static void Register(ZDOData data)
  {
    Cache[data.Name.GetStableHashCode()] = data;
  }
  public static ZDOData? Merge(params ZDOData?[] datas)
  {
    var nonNull = datas.Where(d => d != null).ToArray();
    if (nonNull.Length == 0) return null;
    if (nonNull.Length == 1) return nonNull[0];
    ZDOData result = new();
    foreach (var data in nonNull)
      result.Add(data!);
    return result;
  }
  public ZDOData() { }

  private ZDOData(string data)
  {
    Load(new(data));
  }

  public void Add(ZDOData data)
  {
    foreach (var pair in data.Floats)
      Floats[pair.Key] = pair.Value;
    foreach (var pair in data.Vecs)
      Vecs[pair.Key] = pair.Value;
    foreach (var pair in data.Quats)
      Quats[pair.Key] = pair.Value;
    foreach (var pair in data.Ints)
      Ints[pair.Key] = pair.Value;
    foreach (var pair in data.Longs)
      Longs[pair.Key] = pair.Value;
    foreach (var pair in data.Strings)
      Strings[pair.Key] = pair.Value;
    foreach (var pair in data.ByteArrays)
      ByteArrays[pair.Key] = pair.Value;
  }
  public string Name = "";
  public Dictionary<int, string> Strings = new();
  public Dictionary<int, float> Floats = new();
  public Dictionary<int, int> Ints = new();
  public Dictionary<int, long> Longs = new();
  public Dictionary<int, Vector3> Vecs = new();
  public Dictionary<int, Quaternion> Quats = new();
  public Dictionary<int, byte[]> ByteArrays = new();

  public void Write(ZDO zdo)
  {
    var id = zdo.m_uid;
    if (Floats.Count > 0) ZDOHelper.Init(ZDOExtraData.s_floats, id);
    if (Vecs.Count > 0) ZDOHelper.Init(ZDOExtraData.s_vec3, id);
    if (Quats.Count > 0) ZDOHelper.Init(ZDOExtraData.s_quats, id);
    if (Ints.Count > 0) ZDOHelper.Init(ZDOExtraData.s_ints, id);
    if (Longs.Count > 0) ZDOHelper.Init(ZDOExtraData.s_longs, id);
    if (Strings.Count > 0) ZDOHelper.Init(ZDOExtraData.s_strings, id);
    if (ByteArrays.Count > 0) ZDOHelper.Init(ZDOExtraData.s_byteArrays, id);

    foreach (var pair in Floats)
    {
      if (pair.Value == 0f) continue;
      ZDOExtraData.s_floats[id].SetValue(pair.Key, pair.Value);
    }
    foreach (var pair in Vecs)
      ZDOExtraData.s_vec3[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Quats)
      ZDOExtraData.s_quats[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Ints)
    {
      if (pair.Value == 0) continue;
      ZDOExtraData.s_ints[id].SetValue(pair.Key, pair.Value);
    }
    foreach (var pair in Longs)
    {
      if (pair.Value == 0) continue;
      ZDOExtraData.s_longs[id].SetValue(pair.Key, pair.Value);
    }
    foreach (var pair in Strings)
    {
      if (pair.Value == "") continue;
      ZDOExtraData.s_strings[id].SetValue(pair.Key, pair.Value);
    }
    foreach (var pair in ByteArrays)
      ZDOExtraData.s_byteArrays[id].SetValue(pair.Key, pair.Value);
  }
  public void Load(ZPackage pkg)
  {
    pkg.SetPos(0);
    var num = pkg.ReadInt();
    if ((num & 1) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Floats[pkg.ReadInt()] = pkg.ReadSingle();
    }
    if ((num & 2) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Vecs[pkg.ReadInt()] = pkg.ReadVector3();
    }
    if ((num & 4) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Quats[pkg.ReadInt()] = pkg.ReadQuaternion();
    }
    if ((num & 8) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Ints[pkg.ReadInt()] = pkg.ReadInt();
    }
    // Intended to come before strings (changing would break existing data).
    if ((num & 64) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Longs[pkg.ReadInt()] = pkg.ReadLong();
    }
    if ((num & 16) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Strings[pkg.ReadInt()] = pkg.ReadString();
    }
    if ((num & 128) != 0)
    {
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        ByteArrays[pkg.ReadInt()] = pkg.ReadByteArray();
    }
  }

}