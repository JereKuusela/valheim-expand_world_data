# Territories

The file `expand_territories.yaml` sets available territories and their configuration.

Territories are like biomes but on a separate layer. They are configured separately and then selected from `expand_world.yaml` with the `territory` field.

Same world rule can have both `biome` and `territory` if you want them to line up.

- territory: Identifier for this territory. This is used in `expand_world.yaml`.
- noBuild (default: `false`): If true, players can't build in this territory.
- altitudeMultiplier (default: `1.0`): Multiplier to the terrain altitude (relative to the water level).
- waterDepthMultiplier (default: `1.0`): Multiplies negative terrain altitude.
- minimumAltitude (default: `-1000` meters): Minimum altitude.
- maximumAltitude (default: `10000` meters): Maximum altitude.
- excessFactor (default: `0.5`): How strongly the altitude is reduced if over the maximum or minimum limit. For example 0.5 square roots the excess altitude.
- forestMultiplier (default: `1.0`): Multiplier to the global forest multiplier. This currently does not do anything.
- altitudeDelta (default: `0`): Flat increase/decrease to the terrain altitude.
- colorMap (r,g,b,a): Color in the minimap.
- colorTerrain (r,g,b,a): Terrain style override.
- colorWaterSurface (r,g,b,a): Custom water surface color. Requires "Custom water color" setting enabled in the config.
- colorWaterTop (r,g,b,a): Custom water top color. Requires "Custom water color" setting enabled in the config.
- colorWaterBottom (r,g,b,a): Custom water bottom color. Requires "Custom water color" setting enabled in the config.
- colorWaterShallow (r,g,b,a): Custom shallow water bottom color. Requires "Custom water color" setting enabled in the config.
- statusEffects: List of status effects that are active in this territory.
  - See [Status effects](status-effects.md) for format and more information.

## Examples

New territory that raises the terrain by 100 meters and has a custom minimap color:

```yaml
- territory: Spawn
  colorMap: 0.481, 0.125, 0.125
  colorTerrain: 0, 1, 0, 0
  altitudeDelta: 100
```

Example world rule that applies the territory near the world center:

```yaml
- territory: spawn
  maxDistance: 0.01
```
