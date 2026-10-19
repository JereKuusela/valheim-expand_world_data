using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Service;

namespace ExpandWorldData.Migration;

// Older files used comma separated AND lists and separate "all" player keys.
public static class EventMigration
{
  private static readonly Dictionary<string, string> Renames = new()
  {
    ["notRequiredGlobalKeys"] = "forbiddenGlobalKeys",
    ["notRequiredPlayerKeys"] = "forbiddenPlayerKeys",
    ["notRequiredKnownItems"] = "forbiddenKnownItems",
  };
  private const string All = "requiredPlayerKeysAll";

  public static bool Migrate(string pattern)
  {
    if (!Directory.Exists(Yaml.BaseDirectory)) return false;
    var changedAny = false;
    foreach (var file in Directory.GetFiles(Yaml.BaseDirectory, pattern, SearchOption.AllDirectories))
    {
      var lines = File.ReadAllLines(file).ToList();
      var migrated = MigrateLines(lines);
      if (lines.SequenceEqual(migrated)) continue;
      Migrations.BackupFile(file);
      File.WriteAllLines(file, migrated);
      changedAny = true;
    }
    return changedAny;
  }

  private static string Key(string line)
  {
    var trimmed = line.TrimStart();
    if (trimmed.StartsWith("- ")) trimmed = trimmed.Substring(2).TrimStart();
    var index = trimmed.IndexOf(':');
    return index < 0 ? "" : trimmed.Substring(0, index).Trim();
  }

  private static string Value(string line)
  {
    var index = line.IndexOf(':');
    var value = line.Substring(index + 1);
    var comment = value.IndexOf(" #", StringComparison.Ordinal);
    if (comment >= 0) value = value.Substring(0, comment);
    return value.Trim().Trim('"', '\'');
  }

  private static List<string> MigrateLines(List<string> lines)
  {
    var result = new List<string>();
    var baseIndent = lines.Where(line => line.TrimStart().StartsWith("- ")).Select(line => line.Length - line.TrimStart().Length).DefaultIfEmpty(0).Min();
    var entry = new List<string>();
    foreach (var line in lines)
    {
      var trimmed = line.TrimStart();
      if (trimmed.StartsWith("- ") && line.Length - trimmed.Length == baseIndent)
      {
        result.AddRange(MigrateEntry(entry));
        entry = [];
      }
      entry.Add(line);
    }
    result.AddRange(MigrateEntry(entry));
    return result;
  }

  private static List<string> MigrateEntry(List<string> entry)
  {
    var result = new List<string>(entry);
    var allIndex = result.FindIndex(line => Key(line) == All);
    var anyIndex = result.FindIndex(line => Key(line) == "requiredPlayerKeys");
    var keysIndex = result.FindIndex(line => Key(line) == "requiredGlobalKeys");
    // Only simple one line values can be rewritten safely.
    var all = allIndex >= 0 ? Value(result[allIndex]) : "";
    var any = anyIndex >= 0 ? Value(result[anyIndex]) : "";
    var keys = keysIndex >= 0 ? Value(result[keysIndex]) : "";
    if (keys != "" && !keys.StartsWith("["))
      result[keysIndex] = Replace(result[keysIndex], "requiredGlobalKeys", Rows(keys.Split(',')));
    if (all != "")
    {
      List<string> rows = [];
      if (any != "") rows.Add(any);
      rows.AddRange(all.Split(','));
      if (anyIndex >= 0) result[anyIndex] = Replace(result[anyIndex], "requiredPlayerKeys", Rows(rows));
      else result[allIndex] = Replace(result[allIndex], "requiredPlayerKeys", Rows(rows));
    }
    if (allIndex >= 0 && (all == "" || anyIndex >= 0))
    {
      var removed = result[allIndex];
      result.RemoveAt(allIndex);
      // The list item marker moves to the next field.
      if (removed.TrimStart().StartsWith("- ") && allIndex < result.Count)
      {
        var next = result[allIndex].TrimStart();
        result[allIndex] = removed.Substring(0, removed.Length - removed.TrimStart().Length) + "- " + next;
      }
    }
    for (var i = 0; i < result.Count; i++)
    {
      var key = Key(result[i]);
      if (Renames.TryGetValue(key, out var renamed)) result[i] = result[i].Replace($"{key}:", $"{renamed}:");
    }
    return result;
  }

  private static string Rows(IEnumerable<string> items)
  {
    var serializer = Yaml.Serializer();
    var rows = items.Select(item => item.Trim()).Where(item => item != "").Select(item => $"- {serializer.Serialize(item).TrimEnd('\r', '\n')}").ToList();
    return rows.Count == 0 ? "[]" : string.Join(System.Environment.NewLine, rows);
  }

  private static string Replace(string line, string key, string value)
  {
    var trimmed = line.TrimStart();
    var prefix = line.Substring(0, line.Length - trimmed.Length);
    if (trimmed.StartsWith("- ")) prefix += "- ";
    if (value == "[]") return $"{prefix}{key}: {value}";
    var indent = new string(' ', prefix.Length + 2);
    return $"{prefix}{key}:{System.Environment.NewLine}{indent}{value.Replace(System.Environment.NewLine, System.Environment.NewLine + indent)}";
  }
}
