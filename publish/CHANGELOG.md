- v1.50
  - Adds a new setting "Random locations" to randomize the result of every location reset.
  - Adds a new field `randomSeed` to the `expand_locations.yaml` to randomize the result for every location reset.
  - Improves the config loading to show the actual file and row numbers when an error occurs.

- v1.49
  - Adds lava support for custom biomes.
  - Adds support for lower lava intensity when terrain color alpha is less than 1.
  - Adds new fields `lavaAmount` and `lavaStretch` to the `expand_biomes.yaml` to support lava pattern for custom biomes.
  - Adds a new field `randomSeed` to the `expand_dungeons.yaml` to randomize the result for every dungeon reset.
  - Fixes invisible lava when Ashlands biome is adjacent to some other biome.

- v1.48
  - Fixes blueprint locations not working.

- v1.47
  - Adds support for negative `excessFactor` values in the `expand_biomes.yaml`.
  - Fixed for the new game version.
  - Updated data code to match latest changes from Expand World Prefabs mod.

- v1.46
  - Adds new settings "Ashlands width restriction" and "Ashlands length restriction".
  - Adds new fields `requiredGlobalKeys`, `forbiddenGlobalKeys`, `requiredPlayerKeys` and `forbiddenPlayerKeys` to the `expand_biomes.yaml` to support key based environments.

- v1.45
  - Adds new fields `ashlandsOverride` and `deepNorthOverride` to the `expand_biomes.yaml` to support overriding the Ocean weather in the Ashlands and Deep North areas.
  - Adds a new setting "Restrict Ashlands" to allow removing the Ashlands restrictions.
  - Fixes location object swaps not always working on single player.
  - Fixes chances not working when spawning blueprints with a location object.
  - Fixed for the new update.
