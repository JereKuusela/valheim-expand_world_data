- v1.69
  - Adds new field `randomCardinal` to allow random rotation of locations in cardinal directions (0, 90, 180, 270 degrees). Thanks Kurios.ZeuS!
  - Adds new field `roomLimits` and `maxREtries` to fine tune room amounts in dungeons.
  - Fixes error during start up if biome yaml file didn't exist. Thanks Atlansdaddy!

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
