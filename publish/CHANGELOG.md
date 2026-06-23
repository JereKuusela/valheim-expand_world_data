- v1.66
  - Fixes "distance from similar" for location clones.

- v1.65
  - Adds LocationProxy for blueprint locations so that client side mods can interact with them.
  - Fixes "distance from similar" not automatically working for the same location (should be always considered similar, even without group).

- v1.64
  - Adds new field `groups` to support multiple "min distance from similar" location groups.
  - Adds new field `groupsMax` to support multiple "max distance from similar" location groups.
  - Adds new file for territories, which are similar to biomes but on another layer (experimental).
  - Fixes field `mapColorMultiplier` not working.
  - Fixes location data being mapped to location name instead of location entry (this allows more granular control).

- v1.63
  - Adds new field `pregenerate` to location data, which allows forcing the zone to be generated even when not explored.
  - Fixes vanilla issue of player not cooling down on non-hot biomes (e.g. Ashlands). Now the heat level resets to zero.

- v1.62
  - Fixes data system not working for components (was case sensitive, now correctly case insensitive).
