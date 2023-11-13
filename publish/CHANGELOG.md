- v1.19
  - Adds a new field `clearArea` to the `expand_vegetation.yaml`.
  - Adds a new value `all` to the `randomDamage` field of `expand_locations.yaml` to affect all pieces.
  - Changes the `randomDamage` field of `expand_locations.yaml` to not affect pieces with custom health.

- v1.18
  - Fixes locations sometimes not appearing until relog.

- v1.17
  - Fixed for the new patch.

- v1.16
  - Adds support for connected ZDO to `expand_data.yaml` and `ew_copy` command.
  - Fixes zero or empty values not being applied from `expand_data.yaml`.
  - Fixes `ew_copy` command not working properly for field values.

- v1.15
  - Adds compatibility for Monsternomicon mod (it has slightly wrong data).
  - Fixes command parsing not working if a negative number was subtracted.

- v1.14
  - Fixes error when on server only mode.

- v1.13
  - Removes excessive logging.

- v1.12
  - Fixed for the new patch.
  - Possibly fixes room data not working correctly (didn't test much).

- v1.11
  - Fixes `ew_copy` command not working.
