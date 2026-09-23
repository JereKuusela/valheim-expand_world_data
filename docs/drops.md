# Drops

Drop tables define what items are dropped when creatures or objects are destroyed.

## Configuration

The file `expand_drops.yaml` is created automatically when loading a world. The reference file `expand_drops_reference.yaml` is also generated automatically with examples of all droppable objects. Enable or disable drops from the config using the `Drop data` setting.

Custom drops can be attached to most objects that drop or contain items by setting the `drops` field in [Spawns](spawns.md) or by directly setting the `ews_drops` data.

Drop tables are saved as references, meaning changes retroactively affect existing objects.

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
- biome: List of possible biomes. If the object isn't in a matching biome, this entry is excluded.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome).

### Example

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

### Attaching Drops to Spawns

```yaml
- prefab: Troll
  name: troll_boss
  drops: troll_loot
```

## Debug Commands

Console commands for testing and managing drops:

- `ew_drops` — Forces creation of the drop reference file.
