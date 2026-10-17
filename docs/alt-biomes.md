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
- blockVegetationNames: List of vegetation prefab names that are removed in this modifier.
- blockLocationNames: List of location prefab names that are removed in this modifier.

Client side fields:

- namePrefix: Prefix added to generated names that use this modifier.
- nameSuffix: Suffix added to generated names that use this modifier.
- nameOverride: Replaces the generated name when this modifier is used.
- levelUpChanceMultiplier (default: `1`): Multiplier for creature level up chance in this modifier.
- forceMusic: Music to use in this modifier. If multiple active modifiers set this, the first modifier in the list is used.
- forceEnvironment: [Environment](environments.md) to use in this modifier. If multiple active modifiers set this, the first modifier in the list is used.
- addEnvironments: Extra [environments](environments.md) that can happen in this modifier.
- blockEnvironments: List of [environment](environments.md) names that can't happen in this modifier.
- spawn: Extra creature spawns for this modifier. Uses the same fields as Expand World Spawns.
- blockSpawnNames: List of spawn prefab names that are removed in this modifier.
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

Extra vegetation and locations are added with the `altBiome` field in the [vegetation](vegetation.md) and [locations](locations.md) files.
