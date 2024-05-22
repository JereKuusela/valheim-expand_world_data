using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Data;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public static class Helper
{
  public static List<BlueprintObject> ParseObjects(string[] args)
  {
    return args.Select(s => Parse.Split(s)).Select(split => new BlueprintObject(
        split[0],
        Parse.VectorXZY(split, 1),
        Parse.AngleYXZ(split, 4),
        Parse.VectorXZY(split, 7, Vector3.one),
        DataHelper.Get(split.Length > 11 ? split[11] : ""),
        Parse.Float(split, 10, 1f),
        split.Length > 3 && split[3].ToLowerInvariant() == "snap"
      )).ToList();
  }
  public static float HeightToBaseHeight(float altitude) => altitude / 200f;
  public static float AltitudeToHeight(float altitude) => WorldInfo.WaterLevel + altitude;
  public static float AltitudeToBaseHeight(float altitude) => HeightToBaseHeight(AltitudeToHeight(altitude));
  public static float BaseHeightToAltitude(float baseHeight) => baseHeight * 200f - WorldInfo.WaterLevel;
  public static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
  // Note: Intended that is client when no Znet instance (so stuff isn't loaded in the main menu).
  public static bool IsClient() => !IsServer();
  public static Vector3 RandomValue(Range<Vector3> range)
  {
    if (range.Uniform)
    {
      var multiplier = UnityEngine.Random.Range(0f, 1f);
      return new(
        range.Min.x + (range.Max.x - range.Min.x) * multiplier,
        range.Min.y + (range.Max.y - range.Min.y) * multiplier,
        range.Min.z + (range.Max.z - range.Min.z) * multiplier);
    }
    else
    {
      return new(
        UnityEngine.Random.Range(range.Min.x, range.Max.x),
        UnityEngine.Random.Range(range.Min.y, range.Max.y),
        UnityEngine.Random.Range(range.Min.z, range.Max.z));
    }
  }

  public static bool IsMultiAxis(Range<Vector3> range)
  {
    // Same value would always return the same value.
    if (range.Min == range.Max) return false;
    // Without uniform, each axis would be separate resulting in multiple possible values.
    if (!range.Uniform) return true;
    return range.Min.normalized != Vector3.one || range.Max.normalized != Vector3.one;
  }

  public static string Print(float value)
  {
    return value.ToString("0.#####", NumberFormatInfo.InvariantInfo);
  }
  public static string Print(Vector3 vec)
  {
    return $"{Print(vec.x)},{Print(vec.z)},{Print(vec.y)}";
  }
  public static string Print(Quaternion quat)
  {
    var euler = quat.eulerAngles;
    if (euler.x == 0f && euler.z == 0f)
      return Print(euler.y);
    else
      return $"{Print(euler.y)},{Print(euler.x)},{Print(euler.z)}";
  }

  ///<summary>Converts a list of items to a dictionary, merges duplicates.</summary>
  public static Dictionary<K, V> ToDict<T, K, V>(IEnumerable<T> items, Func<T, K> key, Func<T, V> value)
  {
    return items.ToLookup(key, value).ToDictionary(kvp => kvp.Key, kvp => kvp.First());
  }
  ///<summary>Converts a list of items to a dictionary, merges duplicates.</summary>
  public static HashSet<K> ToSet<T, K>(IEnumerable<T> items, Func<T, K> key)
  {
    return items.Select(key).Distinct().ToHashSet();
  }

  public static bool HasAnyGlobalKey(List<string> keys) => keys.Any(ZoneSystem.instance.m_globalKeys.Contains);
  public static bool HasEveryGlobalKey(List<string> keys) => keys.All(ZoneSystem.instance.m_globalKeys.Contains);

  public static bool IsZero(float a) => Mathf.Abs(a) < 0.001f;
  public static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.001f;
  public static bool ApproxBetween(float a, float min, float max) => min - 0.001f <= a && a <= max + 0.001f;
}

