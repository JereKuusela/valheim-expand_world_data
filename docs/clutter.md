# Clutter

Clutter is the small visual objects, like grass, that are spawned on the terrain.

## Configuration

The file `expand_clutter.yaml` is created automatically when loading a world. Enable or disable clutter from the config using the `Clutter data` setting.

### Clutter Fields

- prefab: Name of the clutter object.
- enabled (default: `true`): Quick way to disable this entry.
- amount (default: `80`): Amount of clutter.
- biome: List of possible biomes.
- instanced (default: `false`): Way of rendering or something. Might cause errors if changed.
- onUncleared (default: `true`): Only on uncleared terrain.
- onCleared (default: `false`): Only on cleared terrain.
- scaleMin (default: `1.0`): Minimum scale for instanced clutter.
- scaleMax (default: `1.0`): Maximum scale for instanced clutter.
- minTilt (default: `0` degrees): Minimum terrain angle.
- maxTilt (default: `10` degrees): Maximum terrain angle.
- minAltitude (default: `-1000` meters): Minimum terrain altitude.
- maxAltitude (default: `10000` meters): Maximum terrain altitude.
- minVegetation (default: `0`): Minimum vegetation mask.
- maxVegetation (default: `0`): Maximum vegetation mask.
- snapToWater (default: `false`): Placed at water level instead of terrain.
- terrainTilt (default: `false`): Rotates with the terrain angle.
- randomOffset (default: `0` meters): Moves the clutter randomly up/down.
- minOceanDepth (default: `0` meters): Minimum water depth (if different from max).
- maxOceanDepth (default: `0` meters): Maximum water depth (if different from min).
- inForest (default: `false`): Only in forests.
- forestTresholdMin (default: `0`): Minimum forest value (if only in forests).
- forestTresholdMax (default: `1`): Maximum forest value (if only in forests).
- fractalScale (default: `0`): Scale when calculating the fractal value.
- fractalOffset (default: `0`): Offset when calculating the fractal value.
- fractalThresholdMin (default: `0`): Minimum fractal value.
- fractalThresholdMax (default: `1`): Maximum fractal value.

## Examples

New clutter entry that adds the Meadows grass also to the Plains. The field `prefab` is required (and `biome` to limit where it is placed):

```yaml
- prefab: instanced_meadows_grass
  biome: Plains
  amount: 100
  instanced: true
  scaleMax: 2
  maxTilt: 25
  minAltitude: 0.4
  maxAltitude: 1000
  terrainTilt: true
  fractalScale: 5
```

Changing an existing entry to have more grass:

```yaml
- prefab: instanced_meadows_grass
  amount: 400
  biome: Meadows, 128
  instanced: true
  scaleMax: 2.3
  maxTilt: 25
  minAltitude: 0.4
  maxAltitude: 1000
  terrainTilt: true
  fractalScale: 5
```

Disabling an existing entry (without removing it from the file):

```yaml
- prefab: clutter_shrub_large
  enabled: false
  amount: 3
  biome: Meadows, 128
```
