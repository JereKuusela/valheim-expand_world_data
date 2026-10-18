# Biomes

Biomes define the terrain, weather and other properties of each area in the world.

## Configuration

The file `expand_biomes.yaml` is created automatically when loading a world. Enable or disable biomes from the config using the `Biome data` setting.

You can add up to 23 new biomes (on top of the 9 default ones).

Note: The game assigns a number for each biome. If some mods don't recognize new biomes you can try using the number instead. The first new biome gets number 1024 which is doubled for each new biome (2nd biome is 2048, 3rd biome is 4096, etc).

### Biome Fields

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
  - requiredGlobalKeys ([list](requirement-lists.md#list)): Active if the world keys are set.
  - forbiddenGlobalKeys ([list](requirement-lists.md#list)): Not active if the world keys match.
  - requiredPlayerKeys ([list](requirement-lists.md#list)): Active if the player keys are set.
  - forbiddenPlayerKeys ([list](requirement-lists.md#list)): Not active if the player keys match.
- maximumAltitude (default: `10000` meters): Maximum altitude.
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
- statusEffects: Array of status effects that are active in this environment.
  - See [Status effects](status-effects.md) for format and more information.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.
- lava (default: `false`): If true, the biome can have lava.
  - Default value is true when `color` r and a values are greater than 0.92.
  - Can be manually set to true for other colors, but the lava is not visually shown.
  - Only Ashlands and Mistlands `terrain` have a lava pattern by default.
  - Other biomes have lava everywhere.
- lavaAmount (default: `1`): Amount of lava in the biome (1 = 100%). Uses Perlin noise.
- lavaStretch (default: `1`): Multiplies the size of lava areas (average total area stays the same).

## Examples

New water biome with clearer height differences on the map and custom water colors. The fields `biome`, `name` and `terrain` are required for new biomes:

```yaml
- biome: AbyssalShelf
  name: Abyssal Shelf
  terrain: Ocean
  environments:
  - environment: Clear
    weight: 5
  - environment: Misty
  mapColorMultiplier: -0.5
  colorMap: 0.2, 0.35, 0.45
  colorWaterSurface: 0.08, 0.22, 0.27, 1
  colorWaterBottom: 0.01, 0.05, 0.07, 1
```

Changing the map color and music of an existing biome:

```yaml
- biome: Meadows
  colorMap: 0.45, 0.7, 0.3
  musicDay: blackforest
```

Conditional environment (only active if none of the players have the `resiststorm` key):

```yaml
- biome: Mountain
  environments:
  - environment: SnowStorm
    weight: 1
  - environment: Snow
    weight: 5
    forbiddenPlayerKeys: resiststorm
```
