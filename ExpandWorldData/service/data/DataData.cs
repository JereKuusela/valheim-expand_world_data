using System.Collections.Generic;
using System.ComponentModel;

namespace Data;

public class DataData
{
  [DefaultValue(null)]
  public string? name;
  [DefaultValue(null)]
  public string? position;
  [DefaultValue(null)]
  public string? rotation;
  [DefaultValue(null)]
  public string? connection;
  [DefaultValue(null)]
  public string[]? bools;
  [DefaultValue(null)]
  public string[]? ints;
  [DefaultValue(null)]
  public string[]? hashes;
  [DefaultValue(null)]
  public string[]? floats;
  [DefaultValue(null)]
  public string[]? strings;
  [DefaultValue(null)]
  public string[]? longs;
  [DefaultValue(null)]
  public string[]? vecs;
  [DefaultValue(null)]
  public string[]? quats;
  [DefaultValue(null)]
  public string[]? bytes;
  [DefaultValue(null)]
  public ItemData[]? items;
  [DefaultValue(null)]
  public string? containerSize;
  [DefaultValue(null)]
  public string? itemAmount;

  [DefaultValue(null)]
  public string? valueGroup;
  [DefaultValue(null)]
  public string? value;
  [DefaultValue(null)]
  public string[]? values;
}

public class ItemData
{
  public string pos = "";
  [DefaultValue(1f)]
  public float chance = 1f;
  [DefaultValue("")]
  public string prefab = "";
  public string? stack;
  public string? quality;
  public string? variant;
  public string? durability;
  public string? crafterID;
  public string? crafterName;
  public string? worldLevel;
  public string? equipped;
  public string? pickedUp;
  public Dictionary<string, string>? customData;
}