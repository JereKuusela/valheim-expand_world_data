using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ExpandWorldData;

/// <summary>
/// Keeps complete alternate-biome vegetation additive without executing the
/// same inherited definition once for every overlapping complete owner.
/// </summary>
internal static class VegetationComposition
{
  private static readonly object IndexSync = new();
  private static readonly FieldInfo[] RowFields = typeof(ZoneSystem.ZoneVegetation)
    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    .Where(field => field.Name != "m_altBiomeParent" && field.Name != "m_foldout")
    .OrderBy(field => field.Name, StringComparer.Ordinal)
    .ToArray();

  private static Dictionary<ZoneSystem.ZoneVegetation, string> FingerprintByRow = [];
  private static Dictionary<string, HashSet<string>> OwnersByFingerprint = new(StringComparer.Ordinal);
  private static object? IndexedVegetationList;
  private static int IndexedVegetationCount = -1;

  internal static void CleanUp()
  {
    lock (IndexSync)
    {
      FingerprintByRow = [];
      OwnersByFingerprint = new(StringComparer.Ordinal);
      IndexedVegetationList = null;
      IndexedVegetationCount = -1;
    }
  }

  internal static void Rebuild(IList<ZoneSystem.ZoneVegetation> rows)
  {
    Dictionary<ZoneSystem.ZoneVegetation, string> fingerprints = [];
    Dictionary<string, HashSet<string>> owners = new(StringComparer.Ordinal);

    foreach (var row in rows)
    {
      if (row == null || !row.m_enable) continue;
      var owner = Owner(row);
      if (owner.Length == 0 || !VegetationLoading.UsesCompleteAltBiome(owner)) continue;
      VegetationSpawning.Extra.TryGetValue(row, out var extra);
      VegetationSpawning.Prefabs.TryGetValue(row, out var prefabs);
      var fingerprint = Fingerprint(row, extra, prefabs);
      fingerprints[row] = fingerprint;
      if (!owners.TryGetValue(fingerprint, out var rowOwners))
      {
        rowOwners = new(StringComparer.Ordinal);
        owners.Add(fingerprint, rowOwners);
      }
      rowOwners.Add(owner);
    }

    lock (IndexSync)
    {
      FingerprintByRow = fingerprints;
      OwnersByFingerprint = owners;
      IndexedVegetationList = rows;
      IndexedVegetationCount = rows.Count;
    }
  }

  internal static bool HasBlockingCompleteOwnerRow(
    ZoneSystem.ZoneVegetation row,
    IEnumerable<AltBiome> altBiomes,
    Func<AltBiome, bool> isBlocked)
  {
    var owner = Owner(row);
    var active = DistinctOwners(altBiomes);

    // A different overlapping owner can still explicitly block this row.
    if (active.Any(alt => !string.Equals(alt.m_name, owner, StringComparison.Ordinal) && isBlocked(alt)))
      return true;

    EnsureIndex();
    Dictionary<ZoneSystem.ZoneVegetation, string> fingerprintByRow;
    Dictionary<string, HashSet<string>> ownersByFingerprint;
    lock (IndexSync)
    {
      fingerprintByRow = FingerprintByRow;
      ownersByFingerprint = OwnersByFingerprint;
    }

    // Preserve the earlier complete-owner behavior if a row was not indexed.
    if (!fingerprintByRow.TryGetValue(row, out var fingerprint) ||
        !ownersByFingerprint.TryGetValue(fingerprint, out var equivalentOwners))
      return false;

    // Native active-owner order is the stable tie-breaker. The first active
    // complete owner contributes a shared definition; later identical copies
    // are skipped. Definitions unique to an active owner remain additive.
    var winner = SelectWinner(active
      .Where(alt => VegetationLoading.UsesCompleteAltBiome(alt.m_name))
      .Select(alt => alt.m_name), equivalentOwners);
    return winner != null && !string.Equals(owner, winner, StringComparison.Ordinal);
  }

  internal static string? SelectWinner(IEnumerable<string> activeOwners, ISet<string> equivalentOwners)
  {
    foreach (var owner in activeOwners)
      if (equivalentOwners.Contains(owner))
        return owner;
    return null;
  }

  private static void EnsureIndex()
  {
    var rows = ZoneSystem.instance?.m_vegetation;
    if (rows == null) return;
    lock (IndexSync)
      if (ReferenceEquals(rows, IndexedVegetationList) && rows.Count == IndexedVegetationCount)
        return;
    Rebuild(rows);
  }

  private static List<AltBiome> DistinctOwners(IEnumerable<AltBiome> altBiomes)
  {
    List<AltBiome> result = [];
    HashSet<string> seen = new(StringComparer.Ordinal);
    foreach (var alt in altBiomes)
      if (alt != null && !string.IsNullOrWhiteSpace(alt.m_name) && seen.Add(alt.m_name))
        result.Add(alt);
    return result;
  }

  private static string Fingerprint(ZoneSystem.ZoneVegetation row, VegetationExtra? extra, List<GameObject>? prefabs)
  {
    StringBuilder text = new(768);
    foreach (var field in RowFields)
    {
      text.Append(field.Name).Append('=');
      AppendValue(text, field.GetValue(row));
      text.Append(';');
    }
    text.Append("extra=");
    AppendValue(text, extra);
    text.Append(";prefabs=");
    AppendValue(text, prefabs);
    return text.ToString();
  }

  private static void AppendValue(StringBuilder text, object? value)
  {
    if (value == null)
    {
      text.Append("null");
      return;
    }
    if (value is string str)
    {
      text.Append('"').Append(str.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
      return;
    }
    if (value is UnityEngine.Object unityObject)
    {
      text.Append(value.GetType().FullName).Append(':').Append(unityObject.name);
      return;
    }
    if (value is float single)
    {
      text.Append(single.ToString("R", CultureInfo.InvariantCulture));
      return;
    }
    if (value is double number)
    {
      text.Append(number.ToString("R", CultureInfo.InvariantCulture));
      return;
    }
    var type = value.GetType();
    if (type.IsPrimitive || type.IsEnum || value is decimal)
    {
      text.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
      return;
    }
    if (value is IEnumerable sequence)
    {
      text.Append('[');
      foreach (var item in sequence)
      {
        AppendValue(text, item);
        text.Append(',');
      }
      text.Append(']');
      return;
    }
    if (type.IsValueType ||
        (type.IsGenericType && type.Name == "Range`1") ||
        string.Equals(type.FullName, typeof(VegetationExtra).FullName, StringComparison.Ordinal))
    {
      text.Append(type.FullName).Append('{');
      foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(field => field.Name, StringComparer.Ordinal))
      {
        text.Append(field.Name).Append('=');
        AppendValue(text, field.GetValue(value));
        text.Append(';');
      }
      text.Append('}');
      return;
    }

    // Named data is cached by EWD. Shared reference identity therefore proves
    // the same data contract; distinct objects remain conservatively unique.
    text.Append(type.FullName).Append("@ref:").Append(RuntimeHelpers.GetHashCode(value));
  }

  private static string Owner(ZoneSystem.ZoneVegetation row) => row.AltBiomeParent ?? "";
}
