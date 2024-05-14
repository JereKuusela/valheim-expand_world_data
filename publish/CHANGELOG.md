
- v1.33
  - Adds new fields for locations and vegetation.
  - Fixed for the new game version.

- v1.32
  - Fixes blueprint rooms not working.
  - Note: This is not Ashlands version yet!

- v1.31
  - Fixes clutter related error when trying to rejoin a world without restarting the game.

- v1.30
  - Fixes arithmetics not working for int or long values.
  - Fixes config directory not always being created.

- v1.29
  - Fixes location clones not working.

- v1.28
  - Adds support for decimals in room sizes.
  - Fixed for the new game version.

- v1.27
  - Adds support for loading data entries from the common data folder (used by World Edit Commands).
  - Adds a new field `items` to the `expand_data.yaml` to support item lists.
  - Adds fields `containerSize` and `itemAmount` to the `expand_data.yaml` to support chest content generation.
  - Adds new fields `locationObjectData`, `locationSwaps`, `dungeonObjectData` and `dungeonSwaps` to the `expand_location.yaml`.
  - Adds value group support to the fields `objectData` and `swaps`.
  - Adds new command `ew_copy_room` and `ew_copy_location` to copy object coordinates relative to the room or location center.
  - Adds new setting "Split data per mod" to create separate data files for each mod.
  - Fixes data fields not automatically updating when modifying the data entries.
  - Removes the `ew_copy` command as obsolete.

- v1.26
  - Fixes multiple options not working with object swaps.
  - Fixes numeric command parameters not working on some computer locales.

- v1.25
  - Fixes zone indices not working on command parameters.

- v1.24
  - Adds icon size support to location icons.

- v1.23
  - Adds Steam/Playfab id support to commands (mainly for Expand World Prefabs).
  - Changes only zero health to be applied as the default health. This allows putting negative health to objects.
  - Changes the keyword format from `{}` to `<>`.
  - Fixes custom room themes not working.
  - floats, ints and longs in `expand_data.yaml` now support ranges and multiple values (randomly selected).
  - strings, hashes and bools in `expand_data.yaml` now support multiple values (randomly selected).

- v1.22
  - Adds support of type `bools` and ``hashes` to the `expand_data.yaml`.
  - Changes the format of command keywords from $$ to {}.
  - Internal change for Expand World Events and Expand World Prefabs.

- v1.21
  - Adds a lot more recognized data keys to `ew_copy`.
  - Improves the connected ZDO support (mainly for Expand World Prefabs).

- v1.20
  - Increases the amount of custom biomes from 19 to 23.
  - Removes the field `centerPiece` as obsolete (exists in blueprints already).
