# Spawns

Spawns are creatures and objects that appear in the world.

## Configuration

The file `expand_spawns.yaml` is created automatically when loading a world. Enable or disable spawns from the config using the `Spawn data` setting.

Note: All distances are in meters, and don't scale up with the world size. For bigger worlds you may need to increase some of the values.

### Spawn Fields

- prefab: Name of the object to spawn. Any [object](https://valheim.wiki/Item_IDs) is valid, not just creatures.
- name: Identifier for this entry, only needed for mod compatibility.
- enabled (default: `true`): Quick way to disable this entry if needed.
- biome: List of possible biomes.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome, 4 = unused).
- spawnChance (default: `100` %): Chance to spawn when attempted.
- maxSpawned: Limit for this entry. Also how many spawn attempts are stacked over time.
- spawnInterval: How often the spawning is attempted.
- minLevel (default: `1`): Minimum creature level.
- maxLevel (default: `1`): Maximum creature level.
- minAltitude (default: `-10000` meters): Minimum terrain altitude.
- maxAltitude (default: `10000` meters): Maximum terrain altitude.
- minDistance (default: `0` meters): Minimum distance from the world center (0 = disabled).
- maxDistance (default: `0` meters): Maximum distance from the world center (0 = disabled).
- spawnAtDay (default: `true`): Enabled during the day time.
- spawnAtNight (default: `true`): Enabled during the night time.
- requiredGlobalKey: Which [global keys](https://valheim.wiki/Global_Keys) must be set to enable this entry.
  - When using format `key value`, the key must have at least this amount of value.
  - After spawning, the key value is reduced by the required value.
  - This can be used to create limited spawns.
  - Creature deaths can be changed to increase the key value by using the `defeatSetGlobalKey` field.
- requiredEnvironments: List of valid environments/weathers.
- spawnDistance (default: `10` meters): Distance to suppress similar spawns.
- spawnRadiusMin (default: `40` meters): Minimum distance from every player.
- spawnRadiusMax (default: `80` meters): Maximum distance from any player.
- groupSizeMin (default: `1`): Minimum amount spawned at the same time.
- groupSizeMax (default: `1`): Maximum amount spawned at the same time.
- groupRadius (default: `3` meters): Radius when spawning multiple objects.
- minTilt (default: `0` degrees): Minimum terrain angle.
- maxTilt (default: `35` degrees): Maximum terrain angle.
- inForest (default: `true`): Enabled in forests.
- outsideForest (default: `true`): Enabled outside forests.
- canSpawnCloseToPlayer (default: `false`): If set to true, spawnRadiusMin is ignored.
- insidePlayerBase (default: `false`): If set to true, player base protection is ignored.
- inLava (default: `false`): If set to true, can spawn in lava.
- outsideLava (default: `true`): If set to false, can only spawn in lava.
- minOceanDepth (default: `0` meters): Minimum ocean depth.
- maxOceanDepth (default: `0` meters): Maximum ocean depth.
- **huntPlayer** (default: `false`): Spawned creatures are more aggressive.
- **groundOffset** (default: `0.5` meters): Spawns above the ground.
- **groundOffsetRandom** (default: `0` meters): Maximum offset from the ground.
- **levelUpMinCenterDistance** (default: `0` meters): Distance from the world center to enable higher creature levels.
- **overrideLevelupChance** (default: `-1` percent): Chance per level up (from the default 10%).
- **faction**: Name of the faction. Requires [Expand World Factions](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Factions/).
- **drops**: Name of the drop table to attach. See [Drops](drops.md) for more info.
- **fields**: Custom fields to override prefab properties. See [Fields](#fields) section below.
- **objects**: Extra objects to spawn. Spawned on top of any obstacles. Format is `id,posX,posZ,posY,chance,data`.

### Fields

Fields can be used to override prefab properties. You can hover a creature and use `data dump=check` from World Edit Commands mod to print available fields to `config/data/data.yaml` file.

```yaml
- prefab: Troll
  fields:
    # Adds kill count tracking for trolls.
    defeatSetGlobalKey: killedtroll ++1
- prefab: Troll
  # Spawning consumes 10 troll kills.
  requiredGlobalKey: killedtroll 10
  fields:
    boss: true
    name: Troll King
    bossEvent: foresttrolls
    health: 1000
    runSpeed: 8
    # "damage" is converted to "RandomSkillFactor", provided for convenience.
    damage: 2
```

The mod attempts to find the correct component and data type automatically. If this doesn't work, you can manually specify the component by using format `component.field` (for example `Character.m_boss`) or the data type by using format `type.key` (for example `float.health`).

## Debug Commands

Console commands for testing and managing spawns:

- `ew_spawns` — Forces creation of the spawn configuration file.
- `ew_test_spawn <name>` — Spawns a creature from the spawn system by entry name. Ignores spawn restrictions.
- `ew_try_spawn <name>` — Attempts to spawn a creature with modified settings (close to player, minimal radius, instant interval).
