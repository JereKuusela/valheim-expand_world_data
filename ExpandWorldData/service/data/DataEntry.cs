using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service;
using UnityEngine;

namespace Data;

// Replicates parametrized ZDO data from Valheim.
public class DataEntry
{
  public DataEntry()
  {
  }
  public DataEntry(string base64)
  {
    Load(new ZPackage(base64));
  }
  public DataEntry(DataData data)
  {
    Load(data);
  }
  public DataEntry(ZDO zdo)
  {
    Load(zdo);
  }

  // Nulls add more code but should be more performant.
  public Dictionary<int, IStringValue>? Strings;
  public Dictionary<int, IFloatValue>? Floats;
  public Dictionary<int, IIntValue>? Ints;
  public Dictionary<int, IBoolValue>? Bools;
  public Dictionary<int, IHashValue>? Hashes;
  public Dictionary<int, ILongValue>? Longs;
  public Dictionary<int, IVector3Value>? Vecs;
  public Dictionary<int, IQuaternionValue>? Quats;
  public Dictionary<int, byte[]>? ByteArrays;
  public List<ItemValue>? Items;
  public Vector2i? ContainerSize;
  private Vector2i GetContainerSize() => ContainerSize ?? new(4, 2);
  public IIntValue? ItemAmount;
  public ZDOExtraData.ConnectionType ConnectionType = ZDOExtraData.ConnectionType.None;
  public int ConnectionHash = 0;
  public IZdoIdValue? OriginalId;
  public IZdoIdValue? TargetConnectionId;
  public IVector3Value? Position;
  public IQuaternionValue? Rotation;

  public void Load(ZDO zdo)
  {
    var id = zdo.m_uid;
    Floats = ZDOExtraData.s_floats.ContainsKey(id) ? ZDOExtraData.s_floats[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    Ints = ZDOExtraData.s_ints.ContainsKey(id) ? ZDOExtraData.s_ints[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    Longs = ZDOExtraData.s_longs.ContainsKey(id) ? ZDOExtraData.s_longs[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    Strings = ZDOExtraData.s_strings.ContainsKey(id) ? ZDOExtraData.s_strings[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    Vecs = ZDOExtraData.s_vec3.ContainsKey(id) ? ZDOExtraData.s_vec3[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    Quats = ZDOExtraData.s_quats.ContainsKey(id) ? ZDOExtraData.s_quats[id].ToDictionary(kvp => kvp.Key, kvp => DataValue.Simple(kvp.Value)) : null;
    ByteArrays = ZDOExtraData.s_byteArrays.ContainsKey(id) ? ZDOExtraData.s_byteArrays[id].ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : null;
    if (ZDOExtraData.s_connectionsHashData.TryGetValue(id, out var conn))
    {
      ConnectionType = conn.m_type;
      ConnectionHash = conn.m_hash;
    }
    OriginalId = new SimpleZdoIdValue(id);
    if (ZDOExtraData.s_connections.TryGetValue(id, out var zdoConn) && zdoConn.m_target != ZDOID.None)
    {
      TargetConnectionId = new SimpleZdoIdValue(zdoConn.m_target);
      ConnectionType = zdoConn.m_type;
    }
  }
  public void Load(DataEntry data)
  {
    if (data.Floats != null)
    {
      Floats ??= [];
      foreach (var pair in data.Floats)
        Floats[pair.Key] = pair.Value;
    }
    if (data.Vecs != null)
    {
      Vecs ??= [];
      foreach (var pair in data.Vecs)
        Vecs[pair.Key] = pair.Value;
    }
    if (data.Quats != null)
    {
      Quats ??= [];
      foreach (var pair in data.Quats)
        Quats[pair.Key] = pair.Value;
    }
    if (data.Ints != null)
    {
      Ints ??= [];
      foreach (var pair in data.Ints)
        Ints[pair.Key] = pair.Value;
    }
    if (data.Strings != null)
    {
      Strings ??= [];
      foreach (var pair in data.Strings)
        Strings[pair.Key] = pair.Value;
    }
    if (data.ByteArrays != null)
    {
      ByteArrays ??= [];
      foreach (var pair in data.ByteArrays)
        ByteArrays[pair.Key] = pair.Value;
    }
    if (data.Longs != null)
    {
      Longs ??= [];
      foreach (var pair in data.Longs)
        Longs[pair.Key] = pair.Value;
    }
    if (data.Bools != null)
    {
      Bools ??= [];
      foreach (var pair in data.Bools)
        Bools[pair.Key] = pair.Value;
    }
    if (data.Hashes != null)
    {
      Hashes ??= [];
      foreach (var pair in data.Hashes)
        Hashes[pair.Key] = pair.Value;
    }
    if (data.Items != null)
    {
      Items ??= [];
      foreach (var item in data.Items)
        Items.Add(item);
    }
    if (data.ContainerSize != null)
      ContainerSize = data.ContainerSize;
    if (data.ItemAmount != null)
      ItemAmount = data.ItemAmount;

    ConnectionType = data.ConnectionType;
    ConnectionHash = data.ConnectionHash;
    OriginalId = data.OriginalId;
    TargetConnectionId = data.TargetConnectionId;
    if (data.Position != null)
      Position = data.Position;
    if (data.Rotation != null)
      Rotation = data.Rotation;
  }
  // Reusing the same object keeps references working.
  public DataEntry Reset(DataData data)
  {
    Floats = null;
    Vecs = null;
    Quats = null;
    Ints = null;
    Strings = null;
    ByteArrays = null;
    Longs = null;
    Bools = null;
    Hashes = null;
    Items = null;
    ContainerSize = null;
    ItemAmount = null;
    ConnectionType = ZDOExtraData.ConnectionType.None;
    ConnectionHash = 0;
    OriginalId = null;
    TargetConnectionId = null;
    Position = null;
    Rotation = null;
    Load(data);
    return this;
  }
  public void Load(DataData data)
  {
    HashSet<string> componentsToAdd = [];
    if (data.floats != null)
    {
      Floats ??= [];
      foreach (var value in data.floats)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse float {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Floats.Add(Hash(kvp.Key), DataValue.Float(kvp.Value));
      }
    }
    if (data.ints != null)
    {
      Ints ??= [];
      foreach (var value in data.ints)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse int {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Ints.Add(Hash(kvp.Key), DataValue.Int(kvp.Value));
      }
    }
    if (data.bools != null)
    {
      Bools ??= [];
      foreach (var value in data.bools)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse bool {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Bools.Add(Hash(kvp.Key), DataValue.Bool(kvp.Value));
      }
    }
    if (data.hashes != null)
    {
      Hashes ??= [];
      foreach (var value in data.hashes)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse hash {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Hashes.Add(Hash(kvp.Key), DataValue.Hash(kvp.Value));
      }
    }
    if (data.longs != null)
    {
      Longs ??= [];
      foreach (var value in data.longs)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse long {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Longs.Add(Hash(kvp.Key), DataValue.Long(kvp.Value));
      }
    }
    if (data.strings != null)
    {
      Strings ??= [];
      foreach (var value in data.strings)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse string {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Strings.Add(Hash(kvp.Key), DataValue.String(kvp.Value));
      }
    }
    if (data.vecs != null)
    {
      Vecs ??= [];
      foreach (var value in data.vecs)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse vector {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Vecs.Add(Hash(kvp.Key), DataValue.Vector3(kvp.Value));
      }
    }
    if (data.quats != null)
    {
      Quats ??= [];
      foreach (var value in data.quats)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse quaternion {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        Quats.Add(Hash(kvp.Key), DataValue.Quaternion(kvp.Value));
      }
    }
    if (data.bytes != null)
    {
      ByteArrays ??= [];
      foreach (var value in data.bytes)
      {
        var kvp = Parse.Kvp(value);
        if (kvp.Key == "") throw new InvalidOperationException($"Failed to parse byte array {value}.");
        if (kvp.Key.Contains("."))
          componentsToAdd.Add(kvp.Key.Split('.')[0]);
        ByteArrays.Add(Hash(kvp.Key), Convert.FromBase64String(kvp.Value));
      }
    }
    if (data.items != null)
    {
      Items = data.items.Select(item => new ItemValue(item)).ToList();
    }
    if (!string.IsNullOrWhiteSpace(data.containerSize))
      ContainerSize = Parse.Vector2Int(data.containerSize!);
    if (!string.IsNullOrWhiteSpace(data.itemAmount))
      ItemAmount = DataValue.Int(data.itemAmount!);
    if (componentsToAdd.Count > 0)
    {
      Ints ??= [];
      Ints[$"HasFields".GetStableHashCode()] = DataValue.Simple(1);
      foreach (var component in componentsToAdd)
        Ints[$"HasFields{component}".GetStableHashCode()] = DataValue.Simple(1);
    }
    if (!string.IsNullOrWhiteSpace(data.position))
      Position = DataValue.Vector3(data.position!);
    if (!string.IsNullOrWhiteSpace(data.rotation))
      Rotation = DataValue.Quaternion(data.rotation!);
    if (!string.IsNullOrWhiteSpace(data.connection))
    {
      var split = Parse.SplitWithEmpty(data.connection!);
      if (split.Length > 1)
      {
        var types = split.Take(split.Length - 1).ToList();
        var hash = split[split.Length - 1];
        ConnectionType = ToByteEnum<ZDOExtraData.ConnectionType>(types);
        // Hacky way, this should be entirely rethought but not much use for the connection system so far.
        if (hash.Contains(":") || hash.Contains("<"))
        {
          TargetConnectionId = DataValue.ZdoId(hash);
          // Must be set to run the connection code.
          OriginalId = TargetConnectionId;
        }
        else
        {
          ConnectionHash = Parse.Int(hash);
          if (ConnectionHash == 0) ConnectionHash = hash.GetStableHashCode();
        }
      }
    }
  }
  public void Load(ZPackage pkg)
  {
    pkg.SetPos(0);
    var num = pkg.ReadInt();
    if ((num & 1) != 0)
    {
      Floats ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Floats[pkg.ReadInt()] = new SimpleFloatValue(pkg.ReadSingle());
    }
    if ((num & 2) != 0)
    {
      Vecs ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Vecs[pkg.ReadInt()] = new SimpleVector3Value(pkg.ReadVector3());
    }
    if ((num & 4) != 0)
    {
      Quats ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Quats[pkg.ReadInt()] = new SimpleQuaternionValue(pkg.ReadQuaternion());
    }
    if ((num & 8) != 0)
    {
      Ints ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Ints[pkg.ReadInt()] = new SimpleIntValue(pkg.ReadInt());
    }
    // Intended to come before strings (changing would break existing data).
    if ((num & 64) != 0)
    {
      Longs ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Longs[pkg.ReadInt()] = new SimpleLongValue(pkg.ReadLong());
    }
    if ((num & 16) != 0)
    {
      Strings ??= [];
      var count = pkg.ReadByte();
      for (var i = 0; i < count; ++i)
        Strings[pkg.ReadInt()] = new SimpleStringValue(pkg.ReadString());
    }
    if ((num & 128) != 0)
    {
      ByteArrays ??= [];
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
  public void Write(Parameters pars, ZDO zdo)
  {
    var id = zdo.m_uid;
    RollItems(pars);
    if (Floats?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_floats, id);
      foreach (var pair in Floats)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_floats[id].SetValue(pair.Key, value.Value);
      }
    }
    if (Vecs?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_vec3, id);
      foreach (var pair in Vecs)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_vec3[id].SetValue(pair.Key, value.Value);

      }
    }
    if (Quats?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_quats, id);
      foreach (var pair in Quats)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_quats[id].SetValue(pair.Key, value.Value);
      }
    }
    if (Ints?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_ints, id);
      foreach (var pair in Ints)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_ints[id].SetValue(pair.Key, value.Value);
      }
    }
    if (Hashes?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_ints, id);
      foreach (var pair in Hashes)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_ints[id].SetValue(pair.Key, value.Value);
      }
    }
    if (Bools?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_ints, id);
      foreach (var pair in Bools)
      {
        var value = pair.Value.GetInt(pars);
        if (value.HasValue)
          ZDOExtraData.s_ints[id].SetValue(pair.Key, value.Value);
      }
    }
    if (Longs?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_longs, id);
      foreach (var pair in Longs)
      {
        var value = pair.Value.Get(pars);
        if (value.HasValue)
          ZDOExtraData.s_longs[id].SetValue(pair.Key, value.Value);

      }
    }
    if (Strings?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_strings, id);
      foreach (var pair in Strings)
      {
        var value = pair.Value.Get(pars);
        if (value != null)
          ZDOExtraData.s_strings[id].SetValue(pair.Key, value);

      }
    }
    if (ByteArrays?.Count > 0)
    {
      ZDOHelper.Init(ZDOExtraData.s_byteArrays, id);
      foreach (var pair in ByteArrays)
        ZDOExtraData.s_byteArrays[id].SetValue(pair.Key, pair.Value);
    }
    HandleConnection(zdo, pars);
    HandleHashConnection(zdo);
    if (Position != null)
    {
      var pos = Position.Get(pars);
      if (pos.HasValue)
      {
        zdo.m_position = pos.Value;
        zdo.SetSector(ZoneSystem.instance.GetZone(pos.Value));
      }
    }
    if (Rotation != null)
    {
      var rot = Rotation.Get(pars);
      if (rot.HasValue)
        zdo.m_rotation = rot.Value.eulerAngles;
    }
  }

  private void HandleConnection(ZDO ownZdo, Parameters pars)
  {
    if (OriginalId == null) return;
    var ownId = ownZdo.m_uid;
    if (TargetConnectionId != null)
    {
      var targetId = TargetConnectionId.Get(pars);
      if (targetId == null) return;
      // If target is known, the setup is easy.
      var otherZdo = ZDOMan.instance.GetZDO(targetId.Value);
      if (otherZdo == null) return;

      ownZdo.SetConnection(ConnectionType, targetId.Value);
      // Portal is two way.
      if (ConnectionType == ZDOExtraData.ConnectionType.Portal)
        otherZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, ownId);

    }
    else
    {
      // Otherwise all zdos must be scanned.
      var originalId = OriginalId.Get(pars);
      if (originalId == null) return;
      var other = ZDOExtraData.s_connections.FirstOrDefault(kvp => kvp.Value.m_target == originalId.Value);
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
  public bool Match(Parameters pars, ZDO zdo)
  {
    if (Strings != null && Strings.Any(pair => pair.Value.Match(pars, zdo.GetString(pair.Key)) == false)) return false;
    if (Floats != null && Floats.Any(pair => pair.Value.Match(pars, zdo.GetFloat(pair.Key)) == false)) return false;
    if (Ints != null && Ints.Any(pair => pair.Value.Match(pars, zdo.GetInt(pair.Key)) == false)) return false;
    if (Longs != null && Longs.Any(pair => pair.Value.Match(pars, zdo.GetLong(pair.Key)) == false)) return false;
    if (Bools != null && Bools.Any(pair => pair.Value.Match(pars, zdo.GetBool(pair.Key)) == false)) return false;
    if (Hashes != null && Hashes.Any(pair => pair.Value.Match(pars, zdo.GetInt(pair.Key)) == false)) return false;
    if (Vecs != null && Vecs.Any(pair => pair.Value.Match(pars, zdo.GetVec3(pair.Key, Vector3.zero)) == false)) return false;
    if (Quats != null && Quats.Any(pair => pair.Value.Match(pars, zdo.GetQuaternion(pair.Key, Quaternion.identity)) == false)) return false;
    if (ByteArrays != null && ByteArrays.Any(pair => pair.Value.SequenceEqual(zdo.GetByteArray(pair.Key)) == false)) return false;
    if (Items != null) return ItemValue.Match(pars, Items, zdo, ItemAmount);
    else if (ItemAmount != null) return ItemValue.Match(pars, zdo, ItemAmount);
    if (ConnectionType != ZDOExtraData.ConnectionType.None && TargetConnectionId != null)
    {
      var conn = zdo.GetConnectionZDOID(ConnectionType);
      var target = TargetConnectionId.Get(pars);
      if (target != null && conn != target) return false;
    }
    return true;
  }
  public bool Unmatch(Parameters pars, ZDO zdo)
  {
    if (Strings != null && Strings.Any(pair => pair.Value.Match(pars, zdo.GetString(pair.Key)) == true)) return false;
    if (Floats != null && Floats.Any(pair => pair.Value.Match(pars, zdo.GetFloat(pair.Key)) == true)) return false;
    if (Ints != null && Ints.Any(pair => pair.Value.Match(pars, zdo.GetInt(pair.Key)) == true)) return false;
    if (Longs != null && Longs.Any(pair => pair.Value.Match(pars, zdo.GetLong(pair.Key)) == true)) return false;
    if (Bools != null && Bools.Any(pair => pair.Value.Match(pars, zdo.GetBool(pair.Key)) == true)) return false;
    if (Hashes != null && Hashes.Any(pair => pair.Value.Match(pars, zdo.GetInt(pair.Key)) == true)) return false;
    if (Vecs != null && Vecs.Any(pair => pair.Value.Match(pars, zdo.GetVec3(pair.Key, Vector3.zero)) == true)) return false;
    if (Quats != null && Quats.Any(pair => pair.Value.Match(pars, zdo.GetQuaternion(pair.Key, Quaternion.identity)) == true)) return false;
    if (ByteArrays != null && ByteArrays.Any(pair => pair.Value.SequenceEqual(zdo.GetByteArray(pair.Key)) == true)) return false;
    if (Items != null) return !ItemValue.Match(pars, Items, zdo, ItemAmount);
    else if (ItemAmount != null) return !ItemValue.Match(pars, zdo, ItemAmount);
    if (ConnectionType != ZDOExtraData.ConnectionType.None && TargetConnectionId != null)
    {
      var conn = zdo.GetConnectionZDOID(ConnectionType);
      var target = TargetConnectionId.Get(pars);
      if (target != null && conn == target) return false;
    }
    return true;
  }

  private static string Format(float value) => value.ToString("0.#####", NumberFormatInfo.InvariantInfo);
  private static string Format(double value) => value.ToString("0.#####", NumberFormatInfo.InvariantInfo);
  public static string PrintVectorXZY(Vector3 vector)
  {
    return vector.x.ToString("0.##", CultureInfo.InvariantCulture) + " " + vector.z.ToString("0.##", CultureInfo.InvariantCulture) + " " + vector.y.ToString("0.##", CultureInfo.InvariantCulture);
  }
  public static string PrintAngleYXZ(Quaternion quaternion)
  {
    return PrintVectorYXZ(quaternion.eulerAngles);
  }
  private static string PrintVectorYXZ(Vector3 vector)
  {
    return vector.y.ToString("0.##", CultureInfo.InvariantCulture) + " " + vector.x.ToString("0.##", CultureInfo.InvariantCulture) + " " + vector.z.ToString("0.##", CultureInfo.InvariantCulture);
  }

  private static T ToByteEnum<T>(List<string> list) where T : struct, Enum
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

  public void RollItems(Parameters pars)
  {
    if (Items?.Count > 0)
    {
      var encoded = ItemValue.LoadItems(pars, Items, GetContainerSize(), ItemAmount?.Get(pars) ?? 0);
      Strings ??= [];
      Strings[ZDOVars.s_items] = DataValue.Simple(encoded);
    }
  }

  public void AddItems(Parameters parameters, ZDO zdo)
  {
    if (Items == null || Items.Count == 0) return;
    var size = GetContainerSize();
    var inv = ItemValue.CreateInventory(zdo, size.x, size.y);
    var items = GenerateItems(parameters, size);
    foreach (var item in items)
      item.AddTo(parameters, inv);
    ZPackage pkg = new();
    inv.Save(pkg);
    zdo.Set(ZDOVars.s_items, pkg.GetBase64());
  }
  public void RemoveItems(Parameters parameters, ZDO zdo)
  {
    if (Items == null || Items.Count == 0) return;
    var inv = ItemValue.CreateInventory(zdo);
    if (inv.m_inventory.Count == 0) return;

    var items = GenerateItems(parameters, new(10000, 10000));
    foreach (var item in items)
      item.RemoveFrom(parameters, inv);
    ZPackage pkg = new();
    inv.Save(pkg);
    zdo.Set(ZDOVars.s_items, pkg.GetBase64());
  }
  public List<ItemValue> GenerateItems(Parameters pars, Vector2i size)
  {
    if (Items == null) throw new ArgumentNullException(nameof(Items));
    return ItemValue.Generate(pars, Items, size, ItemAmount?.Get(pars) ?? 0);
  }

  private static int Hash(string key)
  {
    if (Parse.TryInt(key, out var result)) return result;
    if (key.StartsWith("$", StringComparison.InvariantCultureIgnoreCase))
    {
      var hash = ZSyncAnimation.GetHash(key.Substring(1));
      if (key == "$anim_speed") return hash;
      return 438569 + hash;
    }
    return key.GetStableHashCode();
  }
}
