
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

- v1.44
  - WARNING: This update changes terrain related code. Recommended to back up your save before updating.
  - Adds new fields `wiggleDistanceLength`, `wiggleDistanceWidth`, `wiggleSectorLength`, and `wiggleSectorWidth` to the `expand_world.yaml` to support entry specific wiggling.
  - Fixes the default `expand_world.yaml` file not working.

- v1.43
  - Reverts some unintended changes messing up the terrain height, sorry!

- v1.42
  - Fixes depending EW mods getting broken.

- v1.41
  - Updates the data system to match latest changes and fixes from Expand World Prefabs mod.

- v1.40
  - Fixes blueprint rooms not always loading.
