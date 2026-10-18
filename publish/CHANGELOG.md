- v1.79
  - BREAKING CHANGE: Drop entries now automatically apply to the same prefab as the drop entry name.
  - Adds AND/OR support for "required" and "forbidden" fields. Within row, one requirement must be met. Across rows, all rows must be satisfied. Existing configurations are automatically migrated.
  - Adds new fields `forbiddenEnvironments`, `forbiddenGlobalKey`,`requiredLocation`, `forbiddenLocation`, `requiredObject` and `forbiddenObject` to spawns.
  - Adds new field `forbiddenEnvironments` to events.
  - Renames event required fields to match new naming convention. Existing configurations are automatically migrated.
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

- v1.76
  - Fixes error when trying to override container inventory.
  - Removes obsolete fields `addLocations` and `addVegetation` from altbiomes yaml.

- v1.75
  - Fixes custom location icons not working. Thanks sighsorry!
  - Fixes custom location group system not working.
  - Fixes error on CleanGhostInit (now should how warning instead).
  - Fixes commands `ew_spawns`, `ew_test_spawn` and `ew_try_spawn` missing.
  - Fixes changes to non-world data like clutter triggering full refresh.
  - Reworks data system to match Expand World Prefabs mod.
  - Reworks the world refresh system to be shared with Expand World Size mod (no double refresh).
