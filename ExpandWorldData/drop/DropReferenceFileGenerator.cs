using System.IO;
using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorld.Drops;

public static class ReferenceFileGenerator
{
  public static string Save()
  {
    var scene = ZNetScene.instance;
    if (scene == null)
    {
      Log.Warning("Failed to create drop reference data: ZNetScene not initialized.");
      return "";
    }

    var data = scene.m_namedPrefabs.Values
      .Where(prefab => prefab != null)
      .Select(ToData)
      .Where(entry => entry != null)
      .Select(entry => entry!)
      .OrderBy(entry => entry.name)
      .ToList();

    var yaml = Yaml.Serializer().Serialize(data);
    File.WriteAllText(DropManager.ReferenceFilePath, yaml);
    Log.Info($"Regenerated drop reference file ({data.Count} entries).");
    return yaml;
  }

  private static Data? ToData(GameObject prefab)
  {
    var characterDrop = prefab.GetComponent<CharacterDrop>();
    if (characterDrop != null)
      return ToCharacterDropData(prefab.name, characterDrop);

    var piece = prefab.GetComponent<Piece>();
    if (piece != null)
      return ToPieceData(prefab.name, piece);

    var treeBase = prefab.GetComponent<TreeBase>();
    if (treeBase != null)
      return ToTreeBaseData(prefab.name, treeBase);

    var treeLog = prefab.GetComponent<TreeLog>();
    if (treeLog != null)
      return ToTreeLogData(prefab.name, treeLog);

    if (TryGetDropTable(prefab, out var table))
      return ToDropTableData(prefab.name, table);

    return null;
  }

  private static bool TryGetDropTable(GameObject prefab, out DropTable table)
  {
    var container = prefab.GetComponent<Container>();
    if (container != null && container.m_defaultItems != null)
    {
      table = container.m_defaultItems;
      return true;
    }

    var fish = prefab.GetComponent<Fish>();
    if (fish != null && fish.m_extraDrops != null)
    {
      table = fish.m_extraDrops;
      return true;
    }

    var pickable = prefab.GetComponent<Pickable>();
    if (pickable != null && pickable.m_extraDrops != null)
    {
      table = pickable.m_extraDrops;
      return true;
    }

    var dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
    if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed != null)
    {
      table = dropOnDestroyed.m_dropWhenDestroyed;
      return true;
    }

    var lootSpawner = prefab.GetComponent<LootSpawner>();
    if (lootSpawner != null && lootSpawner.m_items != null)
    {
      table = lootSpawner.m_items;
      return true;
    }

    var mineRock = prefab.GetComponent<MineRock>();
    if (mineRock != null && mineRock.m_dropItems != null)
    {
      table = mineRock.m_dropItems;
      return true;
    }

    var mineRock5 = prefab.GetComponent<MineRock5>();
    if (mineRock5 != null && mineRock5.m_dropItems != null)
    {
      table = mineRock5.m_dropItems;
      return true;
    }

    table = null!;
    return false;
  }

  private static Data ToCharacterDropData(string name, CharacterDrop characterDrop)
  {
    return new Data
    {
      name = name,
      drops = [.. characterDrop.m_drops
        .Where(drop => drop?.m_prefab != null)
        .Select(drop => new DropEntry
        {
          prefab = drop.m_prefab.name,
          chance = drop.m_chance,
          minAmount = drop.m_amountMin,
          maxAmount = drop.m_amountMax,
          dontScale = drop.m_dontScale,
          onePerPlayer = drop.m_onePerPlayer,
          levelMultiplier = drop.m_levelMultiplier,
        })],
    };
  }

  private static Data ToPieceData(string name, Piece piece)
  {
    return new Data
    {
      name = name,
      drops = [.. piece.m_resources
        .Where(requirement => requirement?.m_resItem != null)
        .Select(requirement => new DropEntry
        {
          prefab = requirement.m_resItem.name,
          minAmount = requirement.m_amount,
          maxAmount = requirement.m_amount,
          recover = requirement.m_recover,
        })],
    };
  }

  private static Data ToDropTableData(string name, DropTable? table)
  {
    if (table == null)
      return new Data { name = name };
    return new Data
    {
      name = name,
      chance = table.m_dropChance,
      oneOfEach = table.m_oneOfEach,
      minAmount = table.m_dropMin,
      maxAmount = table.m_dropMax,
      drops = [.. table.m_drops
        .Where(drop => drop.m_item != null)
        .Select(drop => new DropEntry
        {
          prefab = drop.m_item.name,
          dontScale = drop.m_dontScale,
          minStack = drop.m_stackMin,
          maxStack = drop.m_stackMax,
          weight = drop.m_weight,
        })],
    };
  }

  private static Data ToTreeBaseData(string name, TreeBase treeBase)
  {
    var data = ToDropTableData(name, treeBase.m_dropWhenDestroyed);
    data.log = treeBase.m_logPrefab != null ? treeBase.m_logPrefab.name : "none";
    data.stump = treeBase.m_stubPrefab != null ? treeBase.m_stubPrefab.name : "none";
    return data;
  }

  private static Data ToTreeLogData(string name, TreeLog treeLog)
  {
    var data = ToDropTableData(name, treeLog.m_dropWhenDestroyed);
    data.log = treeLog.m_subLogPrefab != null ? treeLog.m_subLogPrefab.name : "none";
    return data;
  }
}
