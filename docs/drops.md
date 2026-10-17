# Drops

Drop tables define what items are dropped when creatures or objects are destroyed.

## Configuration

The file `expand_drops.yaml` is created automatically when loading a world. The reference file `ref_expand_drops.yaml` is also generated automatically with examples of all droppable objects. Enable or disable drops from the config using the `Drop data` setting.

Custom drops can be attached to most objects that drop or contain items by setting the `drops` field in [Spawns](spawns.md) or [Vegetation](vegetation.md) or by directly setting the `ews_drops` data as hash or string.

Drop tables are saved as references, meaning changes retroactively affect existing objects.

If the table `name` matches a prefab name, the table applies automatically to all instances of that prefab. The `ews_drops` data on an object takes priority. For pieces, this also changes the resources returned when deconstructing.

### Drop Table Fields

- name: Identifier used to register the drop data.
- minAmount (default: `1`): Minimum amount of items to drop from the table.
- maxAmount (default: `1`): Maximum amount of items to drop from the table.
- chance (default: `1`): Drop chance for the whole table.
- oneOfEach (default: `false`): Each drop entry is picked at most once.
- biome: List of possible biomes. If the object isn't in a matching biome, the whole override is skipped.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome).
- log: Overrides the log prefab when a tree is chopped down. Use `none` to prevent logs from spawning.
- stump: Overrides the stump when a tree is chopped down. Use `none` to prevent stumps from spawning.
- logData: ZDO data for the spawned log. Can be a data entry name, base64 string, shorthand `type, key, value` or an inline data mapping.
- stumpData: ZDO data for the spawned stump. Same formats as `logData`.
- logDrops: Name of the drop table to attach to the spawned log.
- stumpDrops: Name of the drop table to attach to the spawned stump.
- drops: List of individual item drops.

### Drop Entry Fields

Individual entries within a drop table:

- prefab: Item or object prefab name.
- minAmount (default: `1`): Minimum count for this entry.
- maxAmount (default: `1`): Maximum count for this entry.
- chance (default: `1`): Chance for this individual item entry.
- onePerPlayer (default: `false`): Only one drop per player.
- levelMultiplier (default: `false`): Scale item count by creature level.
- minStack (default: `1`): Minimum stack size for stackable items.
- maxStack (default: `1`): Maximum stack size for stackable items.
- weight (default: `1`): Drop-selection weight used by the table.
- amount (default: `1`): Legacy amount override for some item types.
- recover (default: `true`): Whether the drop is recoverable.
- dontScale (default: `false`): Keep the drop size unscaled.
- data: ZDO data for the spawned item. Same formats as `logData`.
- drops: Name of the drop table to attach to the spawned item.
- biome: List of possible biomes. If the object isn't in a matching biome, this entry is excluded.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome).

## Examples

```yaml
- name: troll_loot
  minAmount: 1
  maxAmount: 2
  chance: 1
  oneOfEach: false
  drops:
    - prefab: Coins
      minAmount: 1
      maxAmount: 5
      chance: 0.75
      minStack: 1
      maxStack: 10
      weight: 1
    - prefab: Leather
      minAmount: 1
      maxAmount: 3
      chance: 0.5
      onePerPlayer: false
      levelMultiplier: false
      minStack: 1
      maxStack: 1
      weight: 2
```

## Attaching drops to a spawn

Drops only applied to Trolls spawned by this spawn configuration.

```yaml
- prefab: Troll
  name: troll_boss
  drops: troll_loot
```

## Attaching drops with Expand World Prefabs mod

Drops only applied to affected Trolls.

```yaml
- prefab: Troll
  type: create
  data: hash, ews_drops, troll_loot
```

## Debug Commands

Console commands for testing and managing drops:

- `ew_drops` — Forces creation of the drop reference file.
