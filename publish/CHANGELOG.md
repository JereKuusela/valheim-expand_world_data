- v1.64
  - Adds new field `groups` to support multiple "min distance" location groups.
  - Adds new field `groupsMax` to support multiple "max distance" location groups.
  - Adds new file for territories, which are similar to biomes but on another layer (experimental).
  - Fixes field `mapColorMultiplier` not working.
  - Fixes location data being mapped to location name instead of location entry (this allows more granular control).

- v1.63
  - Adds new field `pregenerate` to location data, which allows forcing the zone to be generated even when not explored.
  - Fixes vanilla issue of player not cooling down on non-hot biomes (e.g. Ashlands). Now the heat level resets to zero.

- v1.62
  - Fixes data system not working for components (was case sensitive, now correctly case insensitive).

- v1.61
  - Fixes error with Expand World Events caused by previous update.

- v1.60
  - Adds third level to data merging (components).
  - Fixes key based environments not working.
