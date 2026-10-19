using System.Collections.Generic;

namespace ExpandWorldData.Migration;

internal static class VegetationMigration
{
  public static readonly Dictionary<string, string> Renames = new()
  {
    ["requiredGlobalKey"] = "requiredGlobalKeys",
    ["forbiddenGlobalKey"] = "forbiddenGlobalKeys",
  };
}
