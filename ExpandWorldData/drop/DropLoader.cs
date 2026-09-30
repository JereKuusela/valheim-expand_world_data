using System;
using System.Linq;
using ExpandWorldData;

namespace ExpandWorld.Drops;

public static class Loader
{
  public static Data FromData(Data data, string fileName)
  {
    data.biomes = DataManager.ToBiomes(data.biome, fileName);
    data.biomeAreas = DataManager.ToBiomeAreas(data.biomeArea, fileName);
    if (data.log.Equals("none", StringComparison.OrdinalIgnoreCase))
      data.logNone = true;
    else if (data.log != "")
      data.logObj = DataManager.ToPrefab(data.log, fileName);
    if (data.stump.Equals("none", StringComparison.OrdinalIgnoreCase))
      data.stumpNone = true;
    else if (data.stump != "")
      data.stumpObj = DataManager.ToPrefab(data.stump, fileName);
    foreach (var drop in data.drops)
    {
      drop.obj = DataManager.ToPrefab(drop.prefab, fileName);
      if (drop.obj)
        drop.item = drop.obj.GetComponent<ItemDrop>();
      drop.biomes = DataManager.ToBiomes(drop.biome, fileName);
      drop.biomeAreas = DataManager.ToBiomeAreas(drop.biomeArea, fileName);
    }
    return data;
  }
  public static bool IsValid(Data data) => data.drops.All(d => d.obj != null)
    && (data.log == "" || data.logNone || data.logObj != null)
    && (data.stump == "" || data.stumpNone || data.stumpObj != null);
}
