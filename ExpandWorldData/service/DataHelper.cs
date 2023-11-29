using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
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
  public static Dictionary<int, ZDOData> Cache = [];
  public static ZDOData? Create(string data)
  {
    if (data == "") return null;
    var hash = data.GetStableHashCode();
    if (!Cache.ContainsKey(hash))
    {
      try
      {
        Cache[hash] = new ZDOData(data);
      }
      catch
      {
        EWD.Log.LogError($"Failed to find or parse data entry: {data}");
        return null;
      }
    }
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
  public ZDOData(string name, ZDO zdo)
  {
    Name = name;
    Load(zdo);
  }

  private ZDOData(string data)
  {
    Load(new ZPackage(data));
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
  public Dictionary<int, string> Strings = [];
  public Dictionary<int, float> Floats = [];
  public Dictionary<int, int> Ints = [];
  public Dictionary<int, long> Longs = [];
  public Dictionary<int, Vector3> Vecs = [];
  public Dictionary<int, Quaternion> Quats = [];
  public Dictionary<int, byte[]> ByteArrays = [];
  public ZDOExtraData.ConnectionType ConnectionType = ZDOExtraData.ConnectionType.None;
  public int ConnectionHash = 0;
  public ZDOID OriginalId = ZDOID.None;
  public ZDOID TargetConnectionId = ZDOID.None;

  public void Write(ZDO zdo)
  {
    // Hack to allow resetting object health to default.
    // Proper way to remove keys could be supported but so far this is the only use case.
    if (Floats.TryGetValue(ZDOVars.s_health, out var h) && h <= 0f)
      Floats.Remove(ZDOVars.s_health);
    var id = zdo.m_uid;
    if (Floats.Count > 0) ZDOHelper.Init(ZDOExtraData.s_floats, id);
    if (Vecs.Count > 0) ZDOHelper.Init(ZDOExtraData.s_vec3, id);
    if (Quats.Count > 0) ZDOHelper.Init(ZDOExtraData.s_quats, id);
    if (Ints.Count > 0) ZDOHelper.Init(ZDOExtraData.s_ints, id);
    if (Longs.Count > 0) ZDOHelper.Init(ZDOExtraData.s_longs, id);
    if (Strings.Count > 0) ZDOHelper.Init(ZDOExtraData.s_strings, id);
    if (ByteArrays.Count > 0) ZDOHelper.Init(ZDOExtraData.s_byteArrays, id);

    foreach (var pair in Floats)
      ZDOExtraData.s_floats[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Vecs)
      ZDOExtraData.s_vec3[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Quats)
      ZDOExtraData.s_quats[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Ints)
      ZDOExtraData.s_ints[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Longs)
      ZDOExtraData.s_longs[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in Strings)
      ZDOExtraData.s_strings[id].SetValue(pair.Key, pair.Value);
    foreach (var pair in ByteArrays)
      ZDOExtraData.s_byteArrays[id].SetValue(pair.Key, pair.Value);

    HandleConnection(zdo);
    HandleHashConnection(zdo);
  }
  private void HandleConnection(ZDO ownZdo)
  {
    if (OriginalId == ZDOID.None) return;
    var ownId = ownZdo.m_uid;
    if (TargetConnectionId != ZDOID.None)
    {
      // If target is known, the setup is easy.
      var otherZdo = ZDOMan.instance.GetZDO(TargetConnectionId);
      if (otherZdo == null) return;

      ownZdo.SetConnection(ConnectionType, TargetConnectionId);
      // Portal is two way.
      if (ConnectionType == ZDOExtraData.ConnectionType.Portal)
        otherZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, ownId);

    }
    else
    {
      // Otherwise all zdos must be scanned.
      var other = ZDOExtraData.s_connections.FirstOrDefault(kvp => kvp.Value.m_target == OriginalId);
      if (other.Value == null) return;
      var otherZdo = ZDOMan.instance.GetZDO(other.Key);
      if (otherZdo == null) return;
      // Connection is always one way here, otherwise TargetConnectionId would be set.
      otherZdo.SetConnection(other.Value.m_type, ownId);
    }
  }
  private void HandleHashConnection(ZDO ownZdo)
  {
    if (ConnectionHash == 0) return;
    if (ConnectionType == ZDOExtraData.ConnectionType.None) return;
    var ownId = ownZdo.m_uid;

    // Hash data is regenerated on world save.
    // But in this case, it's manually set, so might be needed later.
    ZDOExtraData.SetConnectionData(ownId, ConnectionType, ConnectionHash);

    // While actual connection can be one way, hash is always two way.
    // One of the hashes always has the target type.
    var otherType = ConnectionType ^ ZDOExtraData.ConnectionType.Target;
    var isOtherTarget = (ConnectionType & ZDOExtraData.ConnectionType.Target) == 0;
    var zdos = ZDOExtraData.GetAllConnectionZDOIDs(otherType);
    var otherId = zdos.FirstOrDefault(z => ZDOExtraData.GetConnectionHashData(z, ConnectionType)?.m_hash == ConnectionHash);
    if (otherId == ZDOID.None) return;
    var otherZdo = ZDOMan.instance.GetZDO(otherId);
    if (otherZdo == null) return;
    if ((ConnectionType & ZDOExtraData.ConnectionType.Spawned) > 0)
    {
      // Spawn is one way.
      var connZDO = isOtherTarget ? ownZdo : otherZdo;
      var targetId = isOtherTarget ? otherId : ownId;
      connZDO.SetConnection(ZDOExtraData.ConnectionType.Spawned, targetId);
    }
    if ((ConnectionType & ZDOExtraData.ConnectionType.SyncTransform) > 0)
    {
      // Sync is one way.
      var connZDO = isOtherTarget ? ownZdo : otherZdo;
      var targetId = isOtherTarget ? otherId : ownId;
      connZDO.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, targetId);
    }
    if ((ConnectionType & ZDOExtraData.ConnectionType.Portal) > 0)
    {
      // Portal is two way.
      otherZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, ownId);
      ownZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, otherId);
    }
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
    if ((num & 256) != 0)
    {
      ConnectionType = (ZDOExtraData.ConnectionType)pkg.ReadByte();
      ConnectionHash = pkg.ReadInt();
    }
  }
  public void Load(ZDO zdo)
  {
    var id = zdo.m_uid;
    Floats = ZDOExtraData.s_floats.ContainsKey(id) ? ZDOExtraData.s_floats[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    Ints = ZDOExtraData.s_ints.ContainsKey(id) ? ZDOExtraData.s_ints[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    Longs = ZDOExtraData.s_longs.ContainsKey(id) ? ZDOExtraData.s_longs[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    Strings = ZDOExtraData.s_strings.ContainsKey(id) ? ZDOExtraData.s_strings[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    Vecs = ZDOExtraData.s_vec3.ContainsKey(id) ? ZDOExtraData.s_vec3[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    Quats = ZDOExtraData.s_quats.ContainsKey(id) ? ZDOExtraData.s_quats[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    ByteArrays = ZDOExtraData.s_byteArrays.ContainsKey(id) ? ZDOExtraData.s_byteArrays[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : [];
    if (ZDOExtraData.s_connectionsHashData.TryGetValue(id, out var conn))
    {
      ConnectionType = conn.m_type;
      ConnectionHash = conn.m_hash;
    }
    OriginalId = id;
    if (ZDOExtraData.s_connections.TryGetValue(id, out var zdoConn) && zdoConn.m_target != ZDOID.None)
    {
      TargetConnectionId = zdoConn.m_target;
      ConnectionType = zdoConn.m_type;
    }
  }
}