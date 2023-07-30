using System.ComponentModel;

namespace ExpandWorldData;

public class DataData
{
  public string name = "";
  [DefaultValue(null)]
  public string[]? ints = null;
  [DefaultValue(null)]
  public string[]? floats = null;
  [DefaultValue(null)]
  public string[]? strings = null;
  [DefaultValue(null)]
  public string[]? longs = null;
  [DefaultValue(null)]
  public string[]? vecs = null;
  [DefaultValue(null)]
  public string[]? quats = null;
  [DefaultValue(null)]
  public string[]? bytes = null;
}

public class DefaultData
{
  public static DataData[] Data = new DataData[]{
    new(){
      name = "infinite_health",
      floats = new string[]{"health, 1E30"}
    },
    new(){
      name = "default_health",
      floats = new string[]{"health, 0"}
    },
    new(){
      name = "st_healthy",
      floats = new string[]{"health, 1E30"},
      ints = new string[]{"override_wear, 0"}
    },
    new(){
      name = "st_damaged",
      floats = new string[]{"health, 1E30"},
      ints = new string[]{"override_wear, 1"}
    },
    new(){
      name = "st_broken",
      floats = new string[]{"health, 1E30"},
      ints = new string[]{"override_wear, 3"}
    }
  };
}