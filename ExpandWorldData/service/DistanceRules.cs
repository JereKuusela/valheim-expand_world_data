using System.Collections.Generic;

namespace Service;

public sealed class DistanceGroup(string[] names, float distance)
{
  public readonly string[] Names = names;
  public readonly float Distance = distance;
}

public static class DistanceRules
{
  // Row format: "nameA, nameB, distance, nameC, distance". Distance applies to the names before it.
  public static List<DistanceGroup[]>? Parse(StringList? list, string field, string file)
  {
    if (list == null) return null;
    List<DistanceGroup[]> rows = [];
    foreach (var item in list.Items)
    {
      List<DistanceGroup> groups = [];
      List<string> names = [];
      foreach (var token in Common.Parse.Split(item))
      {
        if (!Common.Parse.TryFloat(token, out var distance))
        {
          names.Add(token);
          continue;
        }
        if (names.Count == 0)
          Log.Warning($"{file}: Invalid {field} value '{item}', distance '{token}' has no names.");
        else
          groups.Add(new([.. names], distance));
        names.Clear();
      }
      if (names.Count > 0)
        Log.Warning($"{file}: Invalid {field} value '{item}', missing distance for '{string.Join(", ", names)}'.");
      if (groups.Count > 0) rows.Add([.. groups]);
    }
    return rows.Count == 0 ? null : rows;
  }
}
