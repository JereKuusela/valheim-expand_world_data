# Vegetation

The file `expand_vegetations.yaml` sets the generated objects. This is a server side feature, clients don't have access to this data.

Changes only apply to unexplored areas. Upgrade World mod can be used to reset areas.

Note: Missing vegetation are automatically added to the file. To disable, set `enabled` to `false` instead of removing anything.

- prefab: Id of the object or name of [blueprint](blueprints.md) file.
  - If multiple prefabs are listed, the prefab is randomly selected.
- enabled (default: `true`): Quick way to disable this entry.
- min (default: `1`): Minimum amount (of groups) to be placed per zone (64m x 64m).
- max (default: `1`): Maximum amount (of groups) to be placed per zone (64m x 64m). If less than 1, has only a chance to appear.
- forcePlacement (default: `false`): By default, only one attempt is made to find a suitable position for each vegetation. If enabled, 50 attempts are done for each vegetation.
- scaleMin (default: `1`): Minimum scale. Number or x,z,y (with y being the height).  
- scaleMax (default: `1`): Maximum scale. Number or x,z,y (with y being the height).
- scaleUniform (default: `true`): If disabled, each axis is scaled independently.
- randTilt (default: `0` degrees): Random rotation within the degrees.
- chanceToUseGroundTilt (default: `0.0`): Chance to set rotation based on terrain angle (from 0.0 to 1.0).
- biome: List of possible biomes.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome, 4 = unused from Valheim data).
- blockCheck (default: `true`): If enabled, clear ground is required.
- minAltitude (default: `0` meters): Minimum terrain altitude.
- maxAltitude (default: `1000` meters): Maximum terrain altitude.
- minOceanDepth (default: `0` meters): Minimum ocean depth (interpolated from zone corners so slightly different from `minAltitude`).
- maxOceanDepth (default: `0` meters): Maximum ocean depth (interpolated from zone corners so slightly different from `maxAltitude`).
- minDistance (default: `0.0` of world radius): Minimum distance from the world center.
- maxDistance (default: `1.0` of world radius): Maximum distance from the world center.
- centerX (default: `0.0`): Custom center point for the distance check.
- centerY (default: `0.0`): Custom center point for the distance check.
- minVegetation (default: `0`): Minimum vegetation mask (random value from 0.0 to 1.0, only used in Ashlands and Mistlands biome).
- maxVegetation (default: `0`): Maximum vegetation mask (random value from 0.0 to 1.0, only used in Ashlands and Mistlands biome).
- surroundCheckVegetation (default: `false`): If enabled, the vegetation is placed near higher vegetation mask.
  - This is sampled within a single zone. The check requires at least 10 samples, so the first 10 spawning attempts always fail.
  - Recommended to use `forcePlacement` for more spawning attempts.
  - The vegetation is placed, if the surrounding vegetation mask is higher than the average mask.
- surroundCheckDistance (default: `20` meters): Distance of the vegetation mask check.
- surroundCheckLayers (default: `2`): How many points are checked.
  - The distance is divided into ring shaped layers.
  - Each layer checks 6 points around the spawn point.
- surroundBetterThanAverage (default: `0`): How much better the mask must be than the average mask.
  - This scales the requirement from the average mask to the highest mask.
  - The value must be less than 1.0 because the mask can't be higher than the highest mask.
  - Negative values can also be used.
- minTilt (default: `0` degrees): Minimum terrain angle.
- maxTilt (default: `90` degrees): Maximum terrain angle.
- terrainDeltaRadius (default: `0` meters): Radius for terrain delta limits.
  - 10 random points are selected within this radius.
  - The altitude difference between the lowest and highest point must be within the limits.
- minTerrainDelta (default: `0` meters): Minimum terrain height change.
  - Higher values cause the vegetation to be based on slopes.
- maxTerrainDelta (default: `10` meters): Maximum terrain height change.
  - Lower values cause the vegetation to be based on flatter areas.
- snapToWater (default: `false`): If enabled, placed at the water level instead of the terrain level.
- snapToStaticSolid (default: `false`): If enabled, placed at the top of solid objects instead of terrain.
- groundOffset (default: `0` meters): Placed above the ground.
- groupSizeMin (default: `1`): Minimum amount to be placed per group.
- groupSizeMax (default: `1`): Maximum amount to be placed per group.
- groupRadius (default: `0` meters): Radius for group placement. This should be less than 32 meters to avoid overflowing to adjacent zones.
- inForest (default: `false`): If enabled, forest thresholds are checked.
  - This creates clusters of vegetation, instead of them being placed evenly.
  - Thresholds between 0 and 1.15 are shown as forest in the minimap.
  - Smaller values would be more closer to the forest center.
  - Larger values would be more away from the forest center.
- forestTresholdMin (default: `0`): Minimum forest value.
- forestTresholdMax (default: `0`): Maximum forest value.
- clearRadius (default: `0` meters): Extra distance away from location clear areas.
  - Note: By default, only the vegetation center point is checked. So big objects can have part of them inside clear areas.
  - If this is a problem, increase the value.
- clearArea (default: `false`): If set, this vegetation creates a clear area similar to locations (based on `clearRadius`).
  - When using this, it's important to put the vegetation at top of the file so that it's generated before other vegetation.
  - Unlike locations, the clear area can't cross zone borders. If the vegetation spawns near the border, it may have a smaller clear area.
  - Use `groupRadius` to move the vegetation away from the zone borders.
- requiredGlobalKey: List of [global keys](https://valheim.fandom.com/wiki/Global_Keys). If all are set, the vegetation is placed.
  - Note: This doesn't affect already generated zones. Intended to be used with Upgrade World + Cron Job mods.
- forbiddenGlobalKey: List of [global keys](https://valheim.fandom.com/wiki/Global_Keys). If any is set, the vegetation is not placed.
  - Note: This doesn't affect already generated zones. Intended to be used with Upgrade World + Cron Job mods.
- data: ZDO data override. For example to create hidden stashes with Spawner Tweaks mod (`object copy` from World Edit Commands).
