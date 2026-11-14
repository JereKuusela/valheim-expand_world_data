- v1.81
  - Fixes compatibility issue with Location Placement Accelerator mod.
  - Fixes lava damage being based on water boiling, now only depends on the biome lava field.
  - Improves performance of water boiling and lava checks.

- v1.80
  - Adds persistent event support (`expand_persistent_events.yaml`).
  - Fixes error when environment aurora blending exceeded Unity's limit.
  - Improves compatibility with other mods. Thanks Warp!

- v1.79
  - BREAKING CHANGE: Drop entries now automatically apply to the same prefab as the drop entry name.
  - Adds AND/OR support for "required" and "forbidden" fields. Within row, one requirement must be met. Across rows, all rows must be satisfied. Existing configurations are automatically migrated.
  - Adds new fields `forbiddenEnvironments`, `forbiddenGlobalKeys`, `requiredLocations`, `forbiddenLocations`, `requiredObjects` and `forbiddenObjects` to spawns.
  - Adds new field `forbiddenEnvironments` to events.
  - Renames event required fields to match new naming convention. Existing configurations are automatically migrated.
  - Renames `requiredGlobalKey` and `forbiddenGlobalKey` of spawns and vegetation to `requiredGlobalKeys` and `forbiddenGlobalKeys`. Existing configurations are automatically migrated.
  - Adds new field `levelOnlyChance` to drop entries.
  - Adds data support to drop entries.
  - Adds shorthand (`type, key, value`) and inline data support to data fields of vegetation, spawns, locations and drops.
  - Fixes alternative biome migration for locations and vegetations possibly affecting custom entries. Thanks warp!
  - Fixes most alternative biomes missing on world load.
  - Fixes Infinity Hammer "Ignore no build" not ignoring no build zones.
  - Fixes custom item data not working on data entries. Thanks Zeall!
  - Optimizes world load to not apply extra refresh.

- v1.78
  - Hotfix.

- v1.77
  - Fixes blueprint locations being in the same group for location "distance from similar" checks.
  - Fixes environments not resetting after changes.
