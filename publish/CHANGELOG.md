- v1.74
  - Adds field `altBiome` to the world data to assign alternative biomes to specific areas.
  - Adds fields `sizeX`, `sizeY`, `rotation` and `wiggleRectangle` to the world data for rectangular areas.
  - Adds field `requiredPersistentEvent` to the spawn data for specifying persistent event requirements.
  - Adds field `addBaseSeedToRandomSpawn` to the dungeon data for including the dungeon base seed in random spawner seeds.
  - Adds field `snowBuildup` to the environment data for specifying snow accumulation levels.
  - Adds fields `auroraIntensity*`, `auroraColors`, `cloudOpacity*`, `aoIntensity*`, `colorAmbientOcclusion` and `psystemsOutsideOnly` to the environment data.
  - Adds fields `interiorRadius`, `interiorEnvironment`, `enemyMinLevel`, `enemyMaxLevel`, `enemyLevelUpChance`, `enemyLevelExcludeGroups` and `blockSpawnGroups` to the location data.
  - Adds syncing of client side location data (`discoverLabel`, `noBuild`, `exteriorRadius`, `interiorRadius`, `interiorEnvironment`, enemy level and spawn group fields).
  - Adds migration for new fields (fixes some Deep North spawns spawning everywhere).
  - Fixes default world data putting Mountains before Deep North.
  - Fixes drops.yaml not being loaded.
  - Fixes some possible data sync issues.
  - Fixes clutter data triggering world regeneration.
  - Improves data reloading to only regenerate the affected parts (terrain, water, map, clutter) instead of the whole world.
  - Removes the `Regenerate map` setting (the map is always regenerated when needed).
  - Optimizes the mod by conditionally patching only necessary parts of the game.

- v1.73
  - Now includes Expand World Spawns and Expand World Events (automatic migration).
  - Adds support for configuring alternative biomes.
  - Fixes world generation being messed up.
  - Fixes custom biomes throwing errors.

- v1.72
  - Fixes edits not clearing biome cache.

- v1.71
  - Fixes for the new game update. Thanks JPValheim!

- v1.70
  - Adds experimental support for blueprint terrain data (only as main object). Thanks sighsorry!
  - Fixes error when trying to scan prefabs with invalid components (for example from other mods).
