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
  public ZDOExtraData.ConnectionType? ConnectionType;
  public int ConnectionHash = 0;
  public IZdoIdValue? OriginalId;
  public IZdoIdValue? TargetConnectionId;
  public IBoolValue? Persistent;
  public IBoolValue? Distant;
  public ZDO.ObjectType? Priority;
  public IVector3Value? Position;
  public IQuaternionValue? Rotation;

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
    if (data.Persistent != null)
      Persistent = data.Persistent;
    if (data.Distant != null)
      Distant = data.Distant;
    if (data.Priority != null)
      Priority = data.Priority;
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
    ConnectionType = null;
    ConnectionHash = 0;
    OriginalId = null;
    TargetConnectionId = null;
    Position = null;
    Rotation = null;
    Distant = null;
    Persistent = null;
    Priority = null;
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
        var hash = Hash(kvp.Key);
        if (Floats.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate float key {kvp.Key}.");
        Floats[hash] = DataValue.Float(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Ints.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate int key {kvp.Key}.");
        Ints[hash] = DataValue.Int(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Bools.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate bool key {kvp.Key}.");
        Bools[hash] = DataValue.Bool(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Hashes.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate hash key {kvp.Key}.");
        Hashes[hash] = DataValue.Hash(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Longs.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate long key {kvp.Key}.");
        Longs[hash] = DataValue.Long(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Strings.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate string key {kvp.Key}.");
        Strings[hash] = DataValue.String(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Vecs.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate vector key {kvp.Key}.");
        Vecs[hash] = DataValue.Vector3(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (Quats.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate quaternion key {kvp.Key}.");
        Quats[hash] = DataValue.Quaternion(kvp.Value);
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
        var hash = Hash(kvp.Key);
        if (ByteArrays.ContainsKey(hash))
          Log.Warning($"Data {data.name}: Duplicate byte array key {kvp.Key}.");
        ByteArrays[hash] = Convert.FromBase64String(kvp.Value);
      }
    }
    if (data.items != null)
    {
      Items = [.. data.items.Select(item => new ItemValue(item))];
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
    if (data.persistent != null)
      Persistent = DataValue.Bool(data.persistent);
    if (data.distant != null)
      Distant = DataValue.Bool(data.distant);
    if (data.priority != null)
      Priority = Enum.TryParse<ZDO.ObjectType>(data.priority, true, out var parsed) ? parsed : null;
    if (!string.IsNullOrWhiteSpace(data.connection))
    {
      var split = Parse.SplitWithEmpty(data.connection!);
      if (split.Length == 1)
      {
        ConnectionType = ToByteEnum<ZDOExtraData.ConnectionType>([.. split]);
      }
      else
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
    if ((num & 512) != 0)
      Persistent = new SimpleBoolValue(pkg.ReadBool());
    if ((num & 1024) != 0)
      Distant = new SimpleBoolValue(pkg.ReadBool());
    if ((num & 2048) != 0)
      Priority = (ZDO.ObjectType)pkg.ReadByte();
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
    if (Persistent != null && Persistent.Match(pars, zdo.Persistent) == false) return false;
    if (Distant != null && Distant.Match(pars, zdo.Distant) == false) return false;
    if (Priority != null && Priority.Value != zdo.Type) return false;
    if (Items != null) return ItemValue.Match(pars, Items, zdo, ItemAmount);
    else if (ItemAmount != null) return ItemValue.Match(pars, zdo, ItemAmount);
    if (ConnectionType.HasValue)
    {
      if (ConnectionType.Value == ZDOExtraData.ConnectionType.None)
      {
        var conn = zdo.GetConnection();
        if (conn != null && conn.m_target != ZDOID.None) return false;
      }
      else
      {
        var conn = zdo.GetConnectionZDOID(ConnectionType.Value);
        if (TargetConnectionId == null)
        {
          if (conn == ZDOID.None) return false;
        }
        else
        {
          var target = TargetConnectionId.Get(pars);
          if (target != null && conn != target) return false;
        }
      }
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
    if (Persistent != null && Persistent.Match(pars, zdo.Persistent) == true) return false;
    if (Distant != null && Distant.Match(pars, zdo.Distant) == true) return false;
    if (Priority != null && Priority.Value == zdo.Type) return false;
    if (Items != null) return !ItemValue.Match(pars, Items, zdo, ItemAmount);
    else if (ItemAmount != null) return !ItemValue.Match(pars, zdo, ItemAmount);
    if (ConnectionType.HasValue)
    {
      if (ConnectionType.Value == ZDOExtraData.ConnectionType.None)
      {
        var conn = zdo.GetConnection();
        if (conn == null || conn.m_target == ZDOID.None) return false;
      }
      else
      {
        var conn = zdo.GetConnectionZDOID(ConnectionType.Value);
        if (TargetConnectionId == null)
        {
          if (conn != ZDOID.None) return false;
        }
        else
        {
          var target = TargetConnectionId.Get(pars);
          if (target != null && conn == target) return false;
        }
      }
    }
    return true;
  }

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

  public static int Hash(string key)
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
    if (Persistent != null)
      zdo.Persistent = Persistent.GetBool(pars) ?? zdo.Persistent;
    if (Distant != null)
      zdo.Distant = Distant.GetBool(pars) ?? zdo.Distant;
    if (Priority != null)
      zdo.Type = Priority.Value;
    HandleConnection(zdo, pars);
    HandleHashConnection(zdo);
    if (Position != null)
    {
      var pos = Position.Get(pars);
      if (pos.HasValue)
      {
        zdo.m_position = pos.Value;
        zdo.SetSector(ZoneSystem.GetZone(pos.Value));
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
    if (ConnectionType == null) return;
    var ownId = ownZdo.m_uid;
    if (TargetConnectionId != null)
    {
      var targetId = TargetConnectionId.Get(pars);
      if (targetId == null) return;
      // If target is known, the setup is easy.
      var otherZdo = ZDOMan.instance.GetZDO(targetId.Value);
      if (otherZdo == null) return;

      ownZdo.SetConnection(ConnectionType.Value, targetId.Value);
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
    if (ConnectionType == null || ConnectionType == ZDOExtraData.ConnectionType.None) return;
    var ownId = ownZdo.m_uid;

    // Hash data is regenerated on world save.
    // But in this case, it's manually set, so might be needed later.
    ZDOExtraData.SetConnectionData(ownId, ConnectionType.Value, ConnectionHash);

    // While actual connection can be one way, hash is always two way.
    // One of the hashes always has the target type.
    var otherType = ConnectionType ^ ZDOExtraData.ConnectionType.Target;
    var isOtherTarget = (ConnectionType & ZDOExtraData.ConnectionType.Target) == 0;
    var zdos = ZDOExtraData.GetAllConnectionZDOIDs(otherType.Value);
    var otherId = zdos.FirstOrDefault(z => ZDOExtraData.GetConnectionHashData(z, ConnectionType.Value)?.m_hash == ConnectionHash);
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
}
