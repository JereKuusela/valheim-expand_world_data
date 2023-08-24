- v1.7
  - Fixes world trying to regenerate on the main menu when changing settings.

- v1.6
  - Adds automatic data migration for dungeons.
  - Fixes Hildir dungeons not generating correctly to the `expand_dungeons.yaml` file.
  - Fixes Hildir dungeon rooms not generating correctly to the `expand_rooms.yaml` file.
  - Fixes the command `ew_rooms` not printing room contents.

- v1.5
  - Fixed for the new patch.
  - Possibly fixes vegetation scale for objects with non-default initial scale.

- v1.4
  - Adds data ids for Marketplace mod.
  - Removes minimap generation from dedicated server.

- v1.3
  - Fixes vanilla rivers not being cleaned up from the world.

- v1.2
  - Fixes multiplayer data sync failure.

- v1.1.1
  - Fixes links in the readme.

- v1.1
  - Adds a new setting for legacy generation (to unfix the generation bug).

- v1.0
  - Initial release. Split from Expand World mod.
  - Adds a new config file `expand_data.yaml`.
  - Adds new fields `scaleMin`, `scaleMax`, and `scaleUniform` to `expand_locations.yaml`.
  - Adds a new setting to disable automatic data migration.
  - Adds a new setting to disable automatic config reload (requires restart to take effect).
  - Changes custom data to be merged from multiple sources (instead the last one overriding).
  - Fixes the default biome configuration being slightly off (most notably for Meadows).
  - Fixes the random damage not working for blueprints.
  - Fixed RNG not being seeded for blueprints.
  - Fixes blueprints not working as extra objects unless they are also locations.¨
  - Fixes location data reload removing Jewelcrafting boss icons.
  - Reworks the status effect system.
