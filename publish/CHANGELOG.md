- v1.22
  - Adds support of type `bools` and ``hashes` to the `expand_data.yaml`.
  - Internal change for Expand World Events.

- v1.21
  - Adds a lot more recognized data keys to `ew_copy`.
  - Improves the connected ZDO support (mainly for Expand World Prefabs).

- v1.20
  - Increases the amount of custom biomes from 19 to 23.
  - Removes the field `centerPiece` as obsolete (exists in blueprints already).

- v1.19
  - Adds a new field `clearArea` to the `expand_vegetation.yaml`.
  - Adds a new value `all` to the `randomDamage` field of `expand_locations.yaml` to affect all pieces.
  - Changes the `randomDamage` field of `expand_locations.yaml` to not affect pieces with custom health.
  - Fixes location reset from Upgrade World mod creating duplicate terrain compilers when terrain leveling was used.
  - Fixes terrain leveling using the modified terrain height, which caused wrong terrain height when Upgrade World was used.
  - Fixes zero health on data not being applied as the default health.
  - Improves performance of terrain leveling.

- v1.18
  - Fixes locations sometimes not appearing until relog.

- v1.17
  - Fixed for the new patch.

- v1.16
  - Adds support for connected ZDO to `expand_data.yaml` and `ew_copy` command.
  - Fixes zero or empty values not being applied from `expand_data.yaml`.
  - Fixes `ew_copy` command not working properly for field values.
