using System.Collections.Generic;
using ExpandWorldData;
using Service;

namespace Data;

public partial class Blueprints
{
  private static partial IEnumerable<string> Folders() => [Configuration.BlueprintGlobalFolder, Configuration.BlueprintLocalFolder];

  private static partial DataEntry? ResolveData(string data, string fileName) => DataHelper.Get(data, fileName);
}

public static partial class BlueprintWatcher
{
  static partial void OnChanged(string name, bool wasLoaded, bool listChanged) => ExpandWorldData.Data.BlueprintManager.Reload(name, wasLoaded);
}

public static partial class BlueprintCache
{
  static partial void OnParsed(Blueprint blueprint) => ExpandWorldData.Data.BlueprintManager.ApplySnapPieces(blueprint);
}
