using System.ComponentModel;
using UnityEngine;

namespace ExpandWorld.Drops;

public class Data
{
  public string name = "";

  [DefaultValue(1)]
  public int minAmount = 1;
  [DefaultValue(1)]
  public int maxAmount = 1;
  [DefaultValue(1f)]
  public float chance = 1f;
  [DefaultValue(false)]
  public bool oneOfEach = false;
  [DefaultValue("")]
  public string biome = "";
  [DefaultValue("")]
  public string biomeArea = "";

  [DefaultValue("")]
  public string log = "";
  [DefaultValue("")]
  public string stump = "";

  // Parsed from biome/biomeArea, not serialized.
  internal Heightmap.Biome biomes = 0;
  internal Heightmap.BiomeArea biomeAreas = 0;

  internal GameObject? logObj = null;
  internal GameObject? stumpObj = null;
  // True when log/stump is set to "none", meaning nothing should spawn at all.
  internal bool logNone = false;
  internal bool stumpNone = false;

  public DropEntry[] drops = [];
}

public class DropEntry
{
  public string prefab = "";

  internal GameObject? obj = null;
  internal ItemDrop? item = null;

  [DefaultValue(1)]
  public int minAmount = 1;
  [DefaultValue(1)]
  public int maxAmount = 1;
  [DefaultValue(1f)]
  public float chance = 1f;
  [DefaultValue(false)]
  public bool onePerPlayer = false;
  [DefaultValue(false)]
  public bool levelMultiplier = false;

  [DefaultValue(1)]
  public int minStack = 1;
  [DefaultValue(1)]
  public int maxStack = 1;
  [DefaultValue(1f)]
  public float weight = 1f;

  [DefaultValue(1)]
  public int amount = 1;
  [DefaultValue(true)]
  public bool recover = true;

  [DefaultValue(false)]
  public bool dontScale = false;

  [DefaultValue("")]
  public string biome = "";
  [DefaultValue("")]
  public string biomeArea = "";

  // Parsed from biome/biomeArea, not serialized.
  internal Heightmap.Biome biomes = 0;
  internal Heightmap.BiomeArea biomeAreas = 0;
}
