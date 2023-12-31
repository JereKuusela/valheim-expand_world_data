using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service;
using UnityEngine;
namespace ExpandWorldData;

public class Range<T>
{
  public T Min;
  public T Max;
  public Range(T value)
  {
    Min = value;
    Max = value;
  }
  public Range(T min, T max)
  {
    Min = min;
    Max = max;
  }
  public bool Uniform = true;
}

///<summary>Contains functions for parsing arguments, etc.</summary>
public static class Parse
{
  public static int Int(string arg, int defaultValue = 0)
  {
    if (!TryInt(arg, out var result))
      return defaultValue;
    return result;
  }
  public static long Long(string arg, long defaultValue = 0)
  {
    if (!long.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
      return defaultValue;
    return result;
  }
  public static int Int(string[] args, int index, int defaultValue = 0)
  {
    if (args.Length <= index) return defaultValue;
    return Int(args[index], defaultValue);
  }
  public static bool TryInt(string arg, out int result)
  {
    return int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
  }
  public static float Float(string arg, float defaultValue = 0f)
  {
    if (!TryFloat(arg, out var result))
      return defaultValue;
    return result;
  }
  public static float Float(string[] args, int index, float defaultValue = 0f)
  {
    if (args.Length <= index) return defaultValue;
    return Float(args[index], defaultValue);
  }
  public static bool TryFloat(string arg, out float result)
  {
    return float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
  }

  public static Quaternion AngleYXZ(string[] args, int index) => AngleYXZ(args, index, Vector3.zero);
  public static Quaternion AngleYXZ(string[] args, int index, Vector3 defaultValue)
  {
    var vector = Vector3.zero;
    vector.y = Float(args, index, defaultValue.y);
    vector.x = Float(args, index + 1, defaultValue.x);
    vector.z = Float(args, index + 2, defaultValue.z);
    return Quaternion.Euler(vector);
  }
  public static List<BlueprintObject> Objects(string[] args)
  {
    return args.Select(s => Split(s)).Select(split => new BlueprintObject(
        split[0],
        VectorXZY(split, 1),
        AngleYXZ(split, 4),
        VectorXZY(split, 7, Vector3.one),
        ZDOData.Create(split.Length > 11 ? split[11] : ""),
        Float(split, 10, 1f),
        split.Length > 3 && split[3].ToLowerInvariant() == "snap"
      )).ToList();
  }
  public static string[] Split(string arg, bool removeEmpty = true, char split = ',') => arg.Split(split).Select(s => s.Trim()).Where(s => !removeEmpty || s != "").ToArray();
  public static string Name(string arg) => arg.Split(':')[0];
  public static Vector3 VectorXZY(string arg) => VectorXZY(arg, Vector3.zero);
  public static Vector3 VectorXZY(string arg, Vector3 defaultValue) => VectorXZY(Split(arg), 0, defaultValue);

  ///<summary>Parses YXZ vector starting at given index. Zero is used for missing values.</summary>
  public static Vector3 VectorXZY(string[] args, int index) => VectorXZY(args, index, Vector3.zero);
  ///<summary>Parses YXZ vector starting at given index. Default values is used for missing values.</summary>
  public static Vector3 VectorXZY(string[] args, int index, Vector3 defaultValue)
  {
    var vector = Vector3.zero;
    vector.x = Float(args, index, defaultValue.x);
    vector.z = Float(args, index + 1, defaultValue.z);
    vector.y = Float(args, index + 2, defaultValue.y);
    return vector;
  }
  public static Vector2 VectorXY(string arg)
  {
    var vector = Vector2.zero;
    var args = Split(arg);
    vector.x = Float(args, 0);
    vector.y = Float(args, 1);
    return vector;
  }
  public static Vector3 Scale(string args) => Scale(Split(args), 0);
  ///<summary>Parses scale starting at zero index. Includes a sanity check and giving a single value for all axis.</summary>
  public static Vector3 Scale(string[] args) => Scale(args, 0);
  ///<summary>Parses scale starting at given index. Includes a sanity check and giving a single value for all axis.</summary>
  public static Vector3 Scale(string[] args, int index) => SanityCheck(VectorXZY(args, index));
  private static Vector3 SanityCheck(Vector3 scale)
  {
    // Sanity check and also adds support for setting all values with a single number.
    if (scale.x == 0) scale.x = 1;
    if (scale.y == 0) scale.y = scale.x;
    if (scale.z == 0) scale.z = scale.x;
    return scale;
  }

  public static Range<string> StringRange(string arg)
  {
    var range = arg.Split('-').ToList();
    if (range.Count > 1 && range[0] == "")
    {
      range[0] = "-" + range[1];
      range.RemoveAt(1);
    }
    if (range.Count > 2 && range[1] == "")
    {
      range[1] = "-" + range[2];
      range.RemoveAt(2);
    }
    if (range.Count == 1) return new(range[0]);
    else return new(range[0], range[1]);

  }
  public static Range<int> IntRange(string arg)
  {
    var range = StringRange(arg);
    return new(Int(range.Min), Int(range.Max));
  }
  public static Range<float> FloatRange(string arg)
  {
    var range = StringRange(arg);
    return new(Float(range.Min), Float(range.Max));
  }
  public static Range<long> LongRange(string arg)
  {
    var range = StringRange(arg);
    return new(Long(range.Min), Long(range.Max));
  }
}
