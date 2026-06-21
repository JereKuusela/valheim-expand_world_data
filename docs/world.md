# World

The file `expand_world.yaml` sets the biome distribution.

Each entry in the file adds a new rule. When determining the biome and territory, the rules are checked one by one from the top until a valid rule is found. This means the order of entries is especially important for this file.

- biome: Identifier of the biome if this rule is valid.
- territory: Identifier of the territory if this rule is valid.
- maxAltitude (default: `1000` meters): Maximum terrain height relative to the water level.
- minAltitude (default: `0` meters if maxAltitude is positive, otherwise `-1000` meters): Minimum terrain height relative to the water level.
- maxDistance (default: `1.0` of world radius): Maximum distance from the world center.
- minDistance (default: `0.0` of world radius): Minimum distance from the world center.
- minSector (default: `0.0` of world angle): Start of the [circle sector](https://en.wikipedia.org/wiki/Circular_sector).
- maxSector (default: `1.0` of world angle): End of the [circle sector](https://en.wikipedia.org/wiki/Circular_sector).
- centerX (default: `0.0` of world radius): Moves the center point away from the world center.
- centerY (default: `0.0` of world radius): Moves the center point away from the world center.
- amount (default: `1.0` of total area): How much of the valid area is randomly filled with this biome. Uses normal distribution, see values below.
- stretch (default: `1.0`): Same as the Stretch biomes setting in Expand World Size but applied just to a single entry. Multiplies the size of biome areas (average total area stays the same).
- seed: Overrides the random outcome of `amount`. Numeric value fixes the outcome. Biome name uses a biome specific value derived from the world seed. No value uses biome from the `terrain` parameter.
- wiggleDistance (default: `true`): Applies "wiggle" to the `minDistance`.
  - Wiggle adds a sin wave pattern to the borders for less artifical biome transitions.
  - Frequency comes from the "Wiggle frequency" config setting.
  - Amplitude comes from the "Wiggle width" config setting.
- wiggleDistanceLength: If set, overrides the "Wiggle frequency" setting.
  - The default value 20 causes 20 wiggles to appear over a full circle.
- wiggleDistanceWidth: If set, overrides the "Wiggle width" setting.
  - The default value 100 meters causes each wiggle to modify the minimum distance from -100 meters to +100 meters.
- wiggleSector (default: `true`): Applies "wiggle" to the `maxSector` and `minSector`.
  - Frequency comes from the "Distance wiggle length" config setting.
  - Amplitude comes from the "Distance wiggle width" config setting.
- wiggleSectorLength: If set, overrides the "Distance wiggle length" setting.
  - The default value 500 meters causes a wiggle to appear every 2 \* Pi \* 500 = 3142 meters.
- wiggleSectorWidth: If set, overrides the "Distance wiggle width" setting.
  - The default value 0.01 causes each wiggle to modify the sector from -0.01 to +0.01.
- boiling (default: `false`): If true, the water is boiling hot.
  - For Ashlands biome, the default value is `true` to keep old configurations working.
  - The boiling effect gradually increases over 300 meters.
    - This can be modified by using a numeric value instead of `true`.
    - For example `0.5` would make the effect increase over 600 meters while `2.0` would make the effect increase over 150 meters.

Note: The world edge is always ocean. This is currently hardcoded.

### Amount

Technically the amount is not a percentage but something closer to a normal distribution.

Manual testing with `ew_biomes` command has given these rough values:

- 0.1: 0.4 %
- 0.2: 2.7 %
- 0.25: 5.3 %
- 0.3: 8.8 %
- 0.35: 14 %
- 0.4: 23 %
- 0.45: 32 %
- 0.5: 42 %
- 0.535: 50 %
- 0.55: 54 %
- 0.6: 64 %
- 0.65: 74 %
- 0.7: 83 %
- 0.75: 90 %
- 0.8: 94 %
- 0.85: 97 %
- 0.9: 99 %

For example if you want to replace 25% of Plains with a new biome you can calculate 0.6 -> 64 % -> 64 % * 0.25 = 16 % -> 0.35. So you would put 0.35 (or 0.36) to the amount of the new biome.

Note: The amount is of the total world size, not of the remaining area. If two biomes have the same seed then their areas will overlap which can lead to unexpected results.

For example if the new biome is a variant of Plains then there is no need to reduce the amount of Plains because the new biome only exists where they would have been Plains.

If the seeds are different, then Plains amount can be calculated with 0.6 -> 64 % -> 64 % * (1 - 0.25) / (1 - 0.16) = 57 % -> 0.56.

### Sectors

Sectors start at the south and increase towards clock-wise direction. So that:

- Bottom left part is between sectors 0 and 0.25.
- Top left part is between sectors 0.25 and 0.5.
- Top right part is between sectors 0.5 and 0.75.
- Top left part is between sectors 0.75 and 1.
- Left part is between sectors 0 and 0.5.
- Top part is between sectors 0.25 and 0.75.
- Right part is between sectors 0.5 and 1.
- Bottom part is between sectors -0.25 and 0.25 (or 0.75 and 1.25).

Note: Of course any number is valid for sectors. Like from 0.37 to 0.62.
