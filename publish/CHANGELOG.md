- v1.68
  - Hotfix: Fixes location and vegetation extra data not working.

- v1.67
  - BREAKING CHANGE: Changes `groups` field to just be a list of groups instead of multiple "min distance from similar".
  - BREAKING CHANGE: Removes `groupsMax` field as obsolete.
  - Adds new fields `closeTo` and `awayFrom` as a separate system to group up or disperse locations.
  - Fixes data loading issue for heavily modded game clients. Thanks Safwan!

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
