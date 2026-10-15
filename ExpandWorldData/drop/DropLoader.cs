using System;
using System.Linq;
using ExpandWorldData;
using DataHelper = global::Data.DataHelper;

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
    data.logDataEntry = WithDrops(DataHelper.Get(data.logData, fileName), data.logDrops);
    data.stumpDataEntry = WithDrops(DataHelper.Get(data.stumpData, fileName), data.stumpDrops);
    foreach (var drop in data.drops)
    {
      drop.obj = DataManager.ToPrefab(drop.prefab, fileName);
      if (drop.obj)
        drop.item = drop.obj.GetComponent<ItemDrop>();
      drop.dataEntry = WithDrops(DataHelper.Get(drop.data, fileName), drop.drops);
      drop.biomes = DataManager.ToBiomes(drop.biome, fileName);
      drop.biomeAreas = DataManager.ToBiomeAreas(drop.biomeArea, fileName);
    }
    return data;
  }
  private static global::Data.DataEntry? WithDrops(global::Data.DataEntry? entry, string drops)
  {
    if (drops == "") return entry;
    global::Data.DataEntry dropsEntry = new()
    {
      Hashes = new System.Collections.Generic.Dictionary<int, global::Data.IHashValue> { { DropManager.HashDrop, global::Data.DataValue.Hash(drops) } }
    };
    return DataHelper.Merge(entry, dropsEntry);
  }
  public static bool IsValid(Data data) => data.drops.All(d => d.obj != null)
    && (data.log == "" || data.logNone || data.logObj != null)
    && (data.stump == "" || data.stumpNone || data.stumpObj != null);
}
