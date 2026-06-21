# Biomes

The file `expand_biomes.yaml` sets available biomes and their configuration.

You can add up to 23 new biomes (on top of the 9 default ones).

Note: The game assigns a number for each biome. If some mods don't recognize new biomes you can try using the number instead. The first new biome gets number 1024 which is doubled for each new biome (2nd biome is 2048, 3rd biome is 4096, etc).

- biome: Identifier for this biome. This is used in the other files.
- name: Display name. Required for new biomes.
- terrain: Identifier of the base biome. Determines which terrain algorithm to use. Required for new biomes.
- nature: Identifier of the base biome. Determines which plants can grow here, whether bees are happy and foot steps. If not given, uses the terrain value.
- altitudeDelta: Flat increase/decrease to the terrain altitude. See Altitude section for more info.
- altitudeMultiplier: Multiplier to the terrain altitude (relative to the water level).
- waterDepthMultiplier (default: `1.0`): Multiplies negative terrain altitude.
- forestMultiplier: Multiplier to the global forest multiplier. Using this requires an extra biome check which will lower the performance.
- environments: List of available environments (weathers) and their relative chances.
  - environment: Name of the environment.
  - weight: Relative chance of this environment.
  - ashlandsOverride (default: `false`): This weather will be used in the Ashlands area instead of the normal weather.
  - deepNorthOverride (default: `false`): This weather will be used in the Deep North area instead of the normal weather.
  - Note: For weather Ashlands_SeaStorm, the default value of `ashlandsOverride` is `true` to keep old configurations working.
  - requiredGlobalKeys: Active if all of these world keys are set.
  - forbiddenGlobalKeys: Active if none of these world keys are set.
  - requiredPlayerKeys: Active if all of these player keys are set.
  - forbiddenPlayerKeys: Active if none of these player keys are set.
- maximumAltitude (default: `1000` meters): Maximum altitude.
- minimumAltitude (default: `-1000` meters): Minimum altitude.
- excessFactor (default: `0.5`): How strongly the altitude is reduced if over the maximum or minimum limit. For example 0.5 square roots the excess altitude.
- paint: Default terrain paint. Format is `dirt,cultivated,paved,vegetation` (from 0.0 to 1.0) or a pre-defined color (cultivated, dirt, grass, grass_dark, patches, paved, paved_dark, paved_dirt, paved_moss)
- colorTerrain (r,g,b,a): Terrain style. Not fully sure how this works but the color value somehow determines which default biome terrain style to use.
- mapColorMultiplier (default: `1.0`): Changes how quickly the terrain altitude affects the map color.
  - Increasing the value can be useful for low altitude biomes to show the altitude differences better.
  - Lowering the value can be useful for high altitude biomes to reduce amount of white color (from mountain altitudes).
  - Negative value can be useful for underwater biomes to show the map color (normally all underwater areas get blueish color).
- colorMap (r,g,b,a): Color in the minimap.
- colorWaterSurface (r,g,b,a): Custom water surface color. Requires "Custom water color" setting enabled in the config.
- colorWaterTop (r,g,b,a): Custom water top color. Requires "Custom water color" setting enabled in the config.
- colorWaterBottom (r,g,b,a): Custom water bottom color. Requires "Custom water color" setting enabled in the config.
- colorWaterShallow (r,g,b,a): Custom shallow water bottom color. Requires "Custom water color" setting enabled in the config.
- musicMorning: Music override for the morning time.
- musicDay: Music override for the day time.
- musicEvening: Music override for the evening time.
- musicNight: Music override for the night time.
- noBuild (default: `false`): If true, players can't build in this biome.
- statusEffects: List of status effects that are active in this environment.
  - See [Status effects](status-effects.md) for format and more information.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.
- lava (default: `false`): If true, the biome can have lava.
  - Default value is true when `color` r and a values are greater than 0.92.
  - Can be manually set to true for other colors, but the lava is not visually shown.
  - Only Ashlands and Mistlands `terrain` have a lava pattern by default.
  - Other biomes have lava everywhere.
- lavaAmount (default: `1`): Amount of lava in the biome (1 = 100%). Uses Perlin noise.
- lavaStretch (default: `1`): Multiplies the size of lava areas (average total area stays the same).

