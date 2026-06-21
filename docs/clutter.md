# Clutter

The file `expand_clutter.yaml` sets the small visual objects.

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
- maxAltitude (default: `1000` meters): Maximum terrain altitude.
- minVegetation (default: `0`): Minimum vegetation mask.
- maxVegetation (default: `0`): Maximum vegetation mask.
- snapToWater (default: `false`): Placed at water level instead of terrain.
- terrainTilt (default: `false`): Rotates with the terrain angle.
- randomOffset (default: `0` meters): Moves the clutter randomly up/down.
- minOceanDepth (default: `0` meters): Minimum water depth (if different from max).
- maxOceanDepth  (default: `0` meters): Maximum water depth (if different from min).
- inForest (default: `false`): Only in forests.
- forestTresholdMin (default: `0`): Minimum forest value (if only in forests).
- forestTresholdMax (default: `0`): Maximum forest value (if only in forests).
- fractalScale  (default: `0`): Scale when calculating the fractal value.
- fractalOffset  (default: `0`): Offset when calculating the fractal value.
- fractalThresholdMin  (default: `0`): Minimum fractal value.
- fractalThresholdMax  (default: `1`): Maximum fractal value.
