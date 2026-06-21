# Locations

The file `expand_locations.yaml` sets the available locations and their placement. This is a server side feature, clients don't have access to this data.

Note: Missing locations are automatically added to the file. To disable, set `enabled` to `false` instead of removing anything.

Note: Each zone (64m x 64m) can only have one size.

See the [wiki](https://valheim.fandom.com/wiki/Points_of_Interest_(POI)) for more info.

Locations are pregenerated at world generation. You must use `genloc` command to redistribute them on unexplored areas after making any changes. For already explored areas, you need to use Upgrade World mod.

- prefab: Identifier of the location object or name of [blueprint](blueprints.md) file. Check wiki for available locations. Hidden ones work too. To create a clone of an existing location, add `:text` to the prefab. For example "Dolmen01:Ghost".
- enabled (default: `true`): Quick way to disable this entry.
- biome: List of possible biomes.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome).
- dungeon: Overrides the default dungeon generator with a custom one from `expand_dungeons.yaml`.
- quantity: Maximum amount. Actual amount is determined if enough suitable positions are found. The base .cfg has a setting to multiply these.
- minDistance (default: `0.0` of world radius): Minimum distance from the world center.
- maxDistance (default: `1.0` of world radius): Maximum distance from the world center.
- minAltitude (default: `0`): Minimum altitude.
- maxAltitude (default: `1000`): Maximum altitude.
- prioritized (default: `false`): Generated first with more attempts.
- centerFirst (default: `false`): Generating is attempted at world center, with gradually moving towards the world edge.
- unique (default: `false`): When placed, all other unplaced locations are removed. Guaranteed maximum of one instance.
- group: Single group name for `minDistanceFromSimilar`.
- groups: Array of group names for `minDistanceFromSimilar`.
  - Format is `group` or `group,distance`.
  - If `distance` is not given, `minDistanceFromSimilar` is used.
- minDistanceFromSimilar (default: `0` meters): Minimum distance between the same location, or locations in the `group` if given.
- groupMax: Single group name for `maxDistanceFromSimilar`.
- groupsMax: Array of group names for `maxDistanceFromSimilar`.
  - Format is `group` or `group,distance`.
  - If `distance` is not given, `maxDistanceFromSimilar` is used.
- maxDistanceFromSimilar (default: `0` meters): Maximum distance between the same location, or locations in the `groupMax` if given.
  - Note: This fails if the location or group doesn't exist. So you need an entry without this check to get started.
- discoverLabel: Shown text when the location is discovered.
- iconAlways: Location icon that is always shown. Use `ew_icons` to see what is available.
  - Format is `icon,size,pulse`.
  - Size 5 or more is considered as meters. These icons scale up and down with the zoom level.
  - Putting anything on the third value causes the icon to pulse. This is not supported with meters.
  - Icon name StartTemple is used as the default spawn point for players. This can be used to spawn players on some other location.
- iconPlaced: Location icon to show when the location is generated. Use `ew_icons` to see what is available.
  - Format is `icon,size,pulse`.
  - Size 5 or more is considered as meters. These icons scale up and down with the zoom level.
  - Putting anything on the third value causes the icon to pulse. This is not supported with meters.
- randomRotation (default: `false`): Randomly rotates the location (unaffected by world seed).
- randomSeed (default: `false`): If true, the generation result is always different instead of depending on the location coordinates.
  - This also randomizes the generated dungeon.
  - To enable this for every location, set "Random locations" to true in the main config.
- slopeRotation (default: `false`): Rotates based on the terrain angle. For example for locations at mountain sides.
- snapToWater (default: `false`): Placed at the water level instead of the terrain.
- minTerrainDelta (default: `0` meters): Minimum nearby terrain height difference.
- maxTerrainDelta (default: `10` meters): Maximum nearby terrain height difference.
- inForest (default: `false`): Only in forests.
- forestTresholdMin (default: `0`): Minimum forest value (if only in forests).
- forestTresholdMax (default: `0`): Maximum forest value (if only in forests).
- groundOffset (default: `0` meters): Placed above the ground.
- minVegetation (default: `0`): Minimum vegetation mask (random value from 0.0 to 1.0, only used in Ashlands and Mistlands biome).
- maxVegetation (default: `1`): Maximum vegetation mask (random value from 0.0 to 1.0, only used in Ashlands and Mistlands biome).
- surroundCheckVegetation (default: `false`): If enabled, the location is placed near higher vegetation mask.
  - This is sampled within the whole firld. The check requires at least 10 samples, so the first 10 spawning attempts always fail.
  - Recommended to use `prioritized` for more spawning attempts.
  - The location is placed, if the surrounding vegetation mask is higher than the average mask.
- surroundCheckDistance (default: `20` meters): Distance of the vegetation mask check.
- surroundCheckLayers (default: `2`): How many points are checked.
  - The distance is divided into ring shaped layers.
  - Each layer checks 6 points around the spawn point.
- surroundBetterThanAverage (default: `0`): How much better the mask must be than the average mask.
  - This scales the requirement from the average mask to the highest mask.
  - The value must be less than 1.0 because the mask can't be higher than the highest mask.
  - Negative values can also be used.
- data: ZDO data override. For example to change altars with Spawner Tweaks mod (`object copy` from World Edit Commands).
- locationObjectSwap: Changes location objects to other objects.
- dungeonObjectSwap: Changes dungeon objects to other objects.
- objectSwap: Changes location and dungeon objects to other objects.
  - See [Object swaps](object-swaps.md) for details.
  - See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).
- locationObjectData: Replaces object data in the location.
- dungeonObjectData: Replaces object data in the dungeon.
- objectData: Replaces object data in the location and the dungeon.
  - See [Object data](object-data.md) for details.
- objects: Extra objects in the location, relative to the location center.
  - See [Custom objects](custom-objects.md) for details.
- randomDamage (default: `false`): If true, pieces without custom health are randomly damaged. If all, all pieces are randomly damaged.
- exteriorRadius: How many meters are cleared, leveled or no build. If not given for blueprints, this is the radius of the blueprint (+ 2 meters).
  - Note: Maximum suggested value is 32 meters. Higher values go past the zone border and can cause issues.
- commands: List of commands that will be executed when spawning the location.
  - Use `<x>`, `<y>` and `<z>` in the command to use the location center point.
  - Use `<a>` in the command to use the location rotation.
  - Basic arithmetic is supported. For example `<x>+10` would add 10 meters to the x coordinate.
- clearArea (default: `false`): If true, vegetation is not placed within `exteriorRadius`.
- noBuild (default: `false`): If true, players can't build within `exteriorRadius`. If number, player can't build within the given radius.
- noBuildDungeon (default: `false`): If true, players can't build inside dungeons within the whole zone. If number, player can't build inside dungeons within the given radius.
  - Note: For bigger dungeons, the number should be set manually (dungeon `bounds` divided by sqrt 2).
- levelArea (default: `true` for blueprints): Flattens the area.
- levelRadius (default: half of `exteriorRadius`): Size of the leveled area.
- levelBorder (default: half of `exteriorRadius`): Adds a smooth transition around the `levelRadius`.
- paint: Paints the terrain. Format is `dirt,cultivated,paved,vegetation` (from 0.0 to 1.0) or a pre-defined color (cultivated, dirt, grass, grass_dark, patches, paved, paved_dark, paved_dirt, paved_moss).
- paintRadius (default: `exteriorRadius`): Size of the painted area.
- paintBorder (default: `5`): Adds a smooth transition around the `paintRadius`.
- scaleMin (default: `1`): Minimum scale. Number or x,z,y (with y being the height).
  - Note: Each object is scaled independently. Distances between objects are not scaled.
  - Note: All objects can't be scaled without using Structure Tweaks mod.
- scaleMax (default: `1`): Maximum scale. Number or x,z,y (with y being the height).
- scaleUniform (default: `true`): If disabled, each axis is scaled independently.
- pregenerate (default: `false`): If true, the zone is automatically generated, even when not explored.
  - This can be useful for Expand World Prefabs mod, if some object in the location needs to be available.

## Examples

Multiple minimum distance groups (location must be away from all of these groups):

```yaml
- prefab: Runestone_Greydwarfs
  groups:
  - Runestones, 250
  - Boo, 100
```

Placed icon configuration:

```yaml
- prefab: Dolmen01
  iconPlaced: Hammer,2,pulse
```

Terrain clear + level operation:

```yaml
- prefab: Dolmen01
  exteriorRadius: 18
  clearArea: true
  levelArea: true
  levelRadius: 8
  levelBorder: 6
```
