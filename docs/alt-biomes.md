# Alternative biomes

Alternative biomes are generated during world generation. They can add extra enemies, vegetation, locations and environments to parts of existing biomes.

## Configuration

The file `expand_altbiomes.yaml` is created automatically when loading a world. Enable or disable alternative biomes from the config using the `Alt biome data` setting.

Alternative biomes can be directly assigned to specific areas with the `altBiome` field in the [world data](world.md).

### Alternative Biome Fields

Server side fields:

- name: Identifier of the alternative biome modifier. Used by locations and vegetation with the `altBiome` field.
- enabled (default: `true`): Quick way to disable this modifier.
- biome: List of base biomes where this modifier can appear. Empty means all configured biomes.
- minDistanceFromCenter (default: `0` meters): Minimum distance from the world center.
- minAmountSpawned (default: `0`): Minimum amount of this modifier that should be generated.
- maxAmountSpawned (default: `0`): Maximum amount of this modifier that can be generated.
- chance (default: `0`): Chance that a valid biome area gets this modifier.
- requireNeighbor (default: `None`): Required neighboring biome. `None` disables this requirement.
- notNeighbor (default: `None`): Forbidden neighboring biome. `None` disables this requirement.
- incompatibleAltBiomes: List of alternative biome names that can't overlap with this modifier.
- minEdgeSize (default: `0` meters): Minimum edge size for a valid modifier area.
- maxEdgeSize (default: `0` meters): Maximum edge size for a valid modifier area.
- minAvgHeight (default: `0` meters): Minimum average terrain height for a valid modifier area.
- maxAvgHeight (default: `0` meters): Maximum average terrain height for a valid modifier area.
- belowWorldX (default: `0`): Maximum X world coordinate. If set, the modifier only appears below this X value.
- aboveWorldX (default: `0`): Minimum X world coordinate. If set, the modifier only appears above this X value.
- belowWorldY (default: `0`): Maximum Y world coordinate. If set, the modifier only appears below this Y value.
- aboveWorldY (default: `0`): Minimum Y world coordinate. If set, the modifier only appears above this Y value.
- blockVegetationNames: List of vegetation prefab names that are removed.
- blockLocationNames: List of location prefab names that are removed.

Client side fields:

- namePrefix: Prefix added to biome name.
- nameSuffix: Suffix added to biome name.
- nameOverride: Replaces the default biome name.
- levelUpChanceMultiplier (default: `1`): Multiplier for creature level up chance.
- forceMusic: Music to use. If multiple active modifiers set this, the first modifier in the list is used.
- forceEnvironment: [Environment](environments.md) to use. If multiple active modifiers set this, the first modifier in the list is used.
- addEnvironments: Extra [environments](environments.md) that can happen.
- blockEnvironments: List of [environment](environments.md) names that can't happen.
- spawn: Extra creature spawns. Uses the same fields as Expand World Spawns.
- blockSpawnNames: List of spawn prefab names that are removed.
- terrainTextureOverride (default: `None`): Biome terrain texture to use for this modifier. `None` keeps the base terrain texture.

## Examples

```yaml
- name: Haunted Meadows
  biome: Meadows
  namePrefix: Haunted
  chance: 0.1
  minDistanceFromCenter: 1000
  maxAmountSpawned: 5
  forceEnvironment: Misty
  blockSpawnNames:
  - Boar
  spawn:
  - prefab: Skeleton
    biome: Meadows
    spawnChance: 25
    maxSpawned: 2
```

World rules (in `expand_world.yaml`) that always add this alternative biome to a sector of the Meadows, instead of relying on the `chance`:

```yaml
- biome: Meadows
  altBiome: Haunted Meadows
  minDistance: 0.2
  maxDistance: 0.4
  minSector: 0.1
  maxSector: 0.3
```

Multiple alternative biomes can be added to the same rule with a comma separated list:

```yaml
- biome: Meadows
  altBiome: Haunted Meadows, Mushroom
  minDistance: 0.2
  maxDistance: 0.4
```

Extra vegetation and locations are added with the `altBiome` field in the [vegetation](vegetation.md) and [locations](locations.md) files.
