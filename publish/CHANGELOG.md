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
