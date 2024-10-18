using System;
using System.Globalization;
using System.Text;
using Service;
using UnityEngine;

namespace Data;

// Parameters are technically just a key-value mapping.
// Proper class allows properly adding caching and other features.
// While also ensuring that all code is in one place.
public class Parameters(string prefab, string arg, Vector3 pos)
{

  protected string[]? args;
  private readonly double time = ZNet.instance.GetTimeSeconds();

  public string Replace(string str)
  {
    StringBuilder parts = new();
    int nesting = 0;
    var start = 0;
    for (int i = 0; i < str.Length; i++)
    {
      if (str[i] == '<')
      {
        if (nesting == 0)
        {
          parts.Append(str.Substring(start, i - start));
          start = i;
        }
        nesting++;

      }
      if (str[i] == '>')
      {
        if (nesting == 1)
        {
          var key = str.Substring(start, i - start + 1);
          parts.Append(ResolveParameters(key));
          start = i + 1;
        }
        if (nesting > 0)
          nesting--;
      }
    }
    if (start < str.Length)
      parts.Append(str.Substring(start));

    return parts.ToString();
  }
  private string ResolveParameters(string str)
  {
    for (int i = 0; i < str.Length; i++)
    {
      var end = str.IndexOf(">", i);
      if (end == -1) break;
      i = end;
      var start = str.LastIndexOf("<", end);
      if (start == -1) continue;
      var length = end - start + 1;
      if (TryReplaceParameter(str.Substring(start, length), out var resolved))
      {
        str = str.Remove(start, length);
        str = str.Insert(start, resolved);
        // Resolved could contain parameters, so need to recheck the same position.
        i = start - 1;
      }
      else
      {
        i = end;
      }
    }
    return str;
  }
  private bool TryReplaceParameter(string key, out string? resolved)
  {
    resolved = GetParameter(key);
    if (resolved == null)
      resolved = ResolveValue(key);
    return resolved != key;
  }

  protected virtual string? GetParameter(string key) =>
    key switch
    {
      "<prefab>" => prefab,
      "<par>" => arg,
      "<par0>" => GetArg(0),
      "<par1>" => GetArg(1),
      "<par2>" => GetArg(2),
      "<par3>" => GetArg(3),
      "<par4>" => GetArg(4),
      "<par5>" => GetArg(5),
      "<par6>" => GetArg(6),
      "<par7>" => GetArg(7),
      "<par8>" => GetArg(8),
      "<par9>" => GetArg(9),
      "<time>" => Format(time),
      "<day>" => EnvMan.instance.GetDay(time).ToString(),
      "<ticks>" => ((long)(time * 10000000.0)).ToString(),
      "<x>" => Format(pos.x),
      "<y>" => Format(pos.y),
      "<z>" => Format(pos.z),
      "<snap>" => Format(WorldGenerator.instance.GetHeight(pos.x, pos.z)),
      _ => null,
    };

  private string GetArg(int index)
  {
    args ??= arg.Split(' ');
    return args.Length <= index ? "" : args[index];
  }
  protected static string Format(float value) => value.ToString("0.#####", NumberFormatInfo.InvariantInfo);
  protected static string Format(double value) => value.ToString("0.#####", NumberFormatInfo.InvariantInfo);

  // Parameter value could be a value group, so that has to be resolved.
  private static string ResolveValue(string value)
  {
    if (!value.StartsWith("<", StringComparison.OrdinalIgnoreCase)) return value;
    if (!value.EndsWith(">", StringComparison.OrdinalIgnoreCase)) return value;
    var sub = value.Substring(1, value.Length - 2);
    if (TryGetValueFromGroup(sub, out var valueFromGroup))
      return valueFromGroup;
    return value;
  }

  private static bool TryGetValueFromGroup(string group, out string value)
  {
    var hash = group.ToLowerInvariant().GetStableHashCode();
    if (!DataLoading.ValueGroups.ContainsKey(hash))
    {
      value = group;
      return false;
    }
    var roll = UnityEngine.Random.Range(0, DataLoading.ValueGroups[hash].Count);
    // Value from group could be another group, so yet another resolve is needed.
    value = ResolveValue(DataLoading.ValueGroups[hash][roll]);
    return true;
  }
}
public class ObjectParameters(string prefab, string arg, ZDO zdo) : Parameters(prefab, arg, zdo.m_position)
{
  private Inventory? inventory;


  protected override string? GetParameter(string key)
  {
    var value = base.GetParameter(key);
    if (value != null) return value;
    value = GetGeneralParameter(key);
    if (value != null) return value;
    var kvp = Parse.Kvp(key, '_');
    if (kvp.Value != "")
    {
      var zdoKey = kvp.Key.Substring(1);
      var zdoValue = kvp.Value.Substring(0, kvp.Value.Length - 1);
      return GetZdoValue(zdoKey, zdoValue);
    }
    return null;
  }

  private string? GetGeneralParameter(string key) =>
    key switch
    {
      "<zdo>" => zdo.m_uid.ToString(),
      "<pos>" => $"{Format(zdo.m_position.x)},{Format(zdo.m_position.z)},{Format(zdo.m_position.y)}",
      "<i>" => ZoneSystem.GetZone(zdo.m_position).x.ToString(),
      "<j>" => ZoneSystem.GetZone(zdo.m_position).y.ToString(),
      "<a>" => Format(zdo.m_rotation.y),
      "<rot>" => $"{Format(zdo.m_rotation.y)},{Format(zdo.m_rotation.x)},{Format(zdo.m_rotation.z)}",
      "<pid>" => GetPid(zdo),
      "<pname>" => GetPname(zdo),
      "<pchar>" => GetPchar(zdo),
      "<owner>" => zdo.GetOwner().ToString(),
      _ => null,
    };

  private static string GetPid(ZDO zdo)
  {
    var peer = GetPeer(zdo);
    if (peer != null)
      return peer.m_rpc.GetSocket().GetHostName();
    else if (Player.m_localPlayer)
      return "Server";
    return "";
  }
  private static string GetPname(ZDO zdo)
  {
    var peer = GetPeer(zdo);
    if (peer != null)
      return peer.m_playerName;
    else if (Player.m_localPlayer)
      return Player.m_localPlayer.GetPlayerName();
    return "";
  }
  private static string GetPchar(ZDO zdo)
  {
    var peer = GetPeer(zdo);
    if (peer != null)
      return peer.m_characterID.ToString();
    else if (Player.m_localPlayer)
      return Player.m_localPlayer.GetPlayerID().ToString();
    return "";
  }
  private static ZNetPeer? GetPeer(ZDO zdo) => zdo.GetOwner() != 0 ? ZNet.instance.GetPeer(zdo.GetOwner()) : null;


  private string GetZdoValue(string key, string value) =>
   key switch
   {
     "key" => DataHelper.GetGlobalKey(value),
     "string" => GetString(value),
     "float" => GetFloat(value).ToString(CultureInfo.InvariantCulture),
     "int" => GetInt(value).ToString(CultureInfo.InvariantCulture),
     "long" => GetLong(value).ToString(CultureInfo.InvariantCulture),
     "bool" => GetBool(value) ? "true" : "false",
     "hash" => ZNetScene.instance.GetPrefab(zdo.GetInt(value))?.name ?? "",
     "vec" => DataEntry.PrintVectorXZY(GetVec3(value)),
     "quat" => DataEntry.PrintAngleYXZ(GetQuaternion(value)),
     "byte" => Convert.ToBase64String(zdo.GetByteArray(value)),
     "zdo" => zdo.GetZDOID(value).ToString(),
     "item" => GetAmountOfItems(value).ToString(),
     "pos" => DataEntry.PrintVectorXZY(GetPos(value)),
     _ => "",
   };

  private string GetString(string value) => ZDOExtraData.s_strings.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var str) ? str : GetStringField(value);
  private float GetFloat(string value) => ZDOExtraData.s_floats.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var f) ? f : GetFloatField(value);
  private int GetInt(string value) => ZDOExtraData.s_ints.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var i) ? i : GetIntField(value);
  private long GetLong(string value) => ZDOExtraData.s_longs.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var l) ? l : GetLongField(value);
  private bool GetBool(string value) => ZDOExtraData.s_ints.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var b) ? b > 0 : GetBoolField(value);
  private Vector3 GetVec3(string value) => ZDOExtraData.s_vec3.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var v) ? v : GetVecField(value);
  private Quaternion GetQuaternion(string value) => ZDOExtraData.s_quats.TryGetValue(zdo.m_uid, out var data) && data.TryGetValue(StringExtensionMethods.GetStableHashCode(value), out var q) ? q : GetQuatField(value);



  private string GetStringField(string value) => GetField(value) is string s ? s : "";
  private float GetFloatField(string value) => GetField(value) is float f ? f : 0;
  private int GetIntField(string value) => GetField(value) is int i ? i : 0;
  private bool GetBoolField(string value) => GetField(value) is bool b ? b : false;
  private long GetLongField(string value) => GetField(value) is long l ? l : 0;
  private Vector3 GetVecField(string value) => GetField(value) is Vector3 v ? v : Vector3.zero;
  private Quaternion GetQuatField(string value) => GetField(value) is Quaternion q ? q : Quaternion.identity;

  private object? GetField(string value)
  {
    var kvp = Parse.Kvp(value, '.');
    if (kvp.Value == "") return null;
    var prefab = ZNetScene.instance.GetPrefab(zdo.m_prefab);
    if (prefab == null) return null;
    // Reflection to get the component and field.
    var component = prefab.GetComponent(kvp.Key);
    if (component == null) return null;
    var fields = kvp.Value.Split('.');
    object result = component;
    foreach (var field in fields)
    {
      var fieldInfo = result.GetType().GetField(field);
      if (fieldInfo == null) return null;
      result = fieldInfo.GetValue(result);
      if (result == null) return null;
    }
    return result;
  }

  private int GetAmountOfItems(string prefab)
  {
    LoadInventory();
    if (inventory == null) return 0;
    int count = 0;
    foreach (var item in inventory.m_inventory)
    {
      if ((item.m_dropPrefab?.name ?? item.m_shared.m_name) == prefab) count += item.m_stack;
    }
    return count;
  }

  private void LoadInventory()
  {
    if (inventory != null) return;
    var currentItems = zdo.GetString(ZDOVars.s_items);
    if (currentItems == "") return;
    inventory = new("", null, 4, 2);
    inventory.Load(new ZPackage(currentItems));
  }

  private Vector3 GetPos(string value)
  {
    var offset = Parse.VectorXZY(value);
    return zdo.GetPosition() + zdo.GetRotation() * offset;
  }
}
