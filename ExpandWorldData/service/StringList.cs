using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Service;

// Accepts either a single string or a list of strings. Each item is a row, commas separate entries inside a row.
// Rows are combined with AND, entries inside a row with OR.
public sealed class StringList(string[] items)
{
  public readonly string[] Items = items;

  public List<string[]> Rows() => [.. Items.Select(item => Parse.Split(item)).Where(row => row.Length > 0)];

  public static StringList? FromRows(IEnumerable<string> rows)
  {
    var items = rows.Where(row => row != "").ToArray();
    return items.Length == 0 ? null : new StringList(items);
  }

  public static List<string[]>? ToRows(StringList? list)
  {
    var rows = list?.Rows();
    return rows == null || rows.Count == 0 ? null : rows;
  }

  public static bool Pass(List<string[]> rows, Func<string, bool> has) => rows.All(row => row.Any(has));

  // Same logic as required, so an empty list forbids nothing.
  public static bool Forbids(List<string[]>? rows, Func<string, bool> has) => rows != null && rows.Count > 0 && Pass(rows, has);
}

internal sealed class StringListConverter : IYamlTypeConverter
{
  public bool Accepts(Type type) => type == typeof(StringList);

  public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
  {
    if (parser.Current is Scalar)
    {
      var text = (string?)rootDeserializer(typeof(string));
      return string.IsNullOrWhiteSpace(text) ? null : new StringList([text!]);
    }
    if (parser.Current is SequenceStart)
    {
      var items = (string[]?)rootDeserializer(typeof(string[])) ?? [];
      return items.Length == 0 ? null : new StringList(items);
    }
    rootDeserializer(typeof(object));
    return null;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
  {
    if (value is not StringList list) return;
    if (list.Items.Length == 1) serializer(list.Items[0], typeof(string));
    else serializer(list.Items, typeof(string[]));
  }
}
