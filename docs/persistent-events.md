# Persistent events

Persistent events are long lasting events that cover an area of the world (for example the Bog Witch event). They are saved to the world and spawn objects, change the environment and enable creature spawns inside their area.

## Configuration

The file `expand_persistent_events.yaml` is created automatically when loading a world. Enable or disable the data from the config using the `Persistent event data` setting.

Vanilla console commands work with the data: `pevents list`, `pevents start [name]`, `pevents stop [name]` and `pevents here`.

### Warning

Active events are saved to the world by their position in the file. Removing or reordering entries changes which event the saved events refer to. Use `enabled: false` instead of removing entries. Active events pointing to missing entries are removed when the world is loaded.

## Fields

- name: Unique name. Used by commands and by the `requiredPersistentEvent` field of spawns.
- enabled (default: `true`): If false, the event can't be started.
- mapToken (default: `$unknown_event`): Name shown on the minimap.
- minRadius (default: `100`), maxRadius (default: `300`): Radius of the event area in meters.
- maxConcurrent (default: `-1`): How many of this event can be active at the same time. Negative for no limit.
- location (default: empty): Location that is spawned at the event position (only in zones without a location).
- minDuration, maxDuration (default: `0`): Duration in seconds. If both are 0, the event doesn't expire.
- biome (default: all): Biomes where the event can be placed.
- minDistance (default: `1000`), maxDistance (default: `8000`): Distance from the world center.
- minAltitude (default: `0`), maxAltitude (default: `970`): Altitude from the sea level.
- maxSlope (default: `45`): Maximum slope in degrees.
- minDistanceFromSimilar (default: `400`): Minimum distance to other active events of the same type.
- environment (default: empty): Environment forced inside the event area.
- perBiomeEnvironments (default: `false`): If true, the biome name is added to the environment (for example `Storm_Meadows`).
- graphicalEffects (default: empty): Visual effects, currently only `Snow`.
- customChance (default: `0`): Chance in percent to start the event on each roll.
- customInterval (default: `0`): Seconds between rolls. If 0, the event is never started automatically.
- requiredGlobalKeys, forbiddenGlobalKeys (default: empty): Global keys that must be set / must not be set for an automatic start. See [Requirement lists](requirement-lists.md).
- startMessage, endMessage (default: empty): Center message shown to all players when the event starts / ends.
- startCommands, endCommands (default: empty): Commands ran when the event starts / ends. Supports `<x>`, `<y>`, `<z>`, `<i>` and `<j>` for the event position.
- objects: Objects spawned when the event starts.

Automatic rolls are not saved, so the timers restart when the server restarts.

### Objects

- prefabs: Prefab or blueprint names. One is picked randomly for each object.
- data (default: empty): Data entry applied to the spawned objects. See [Custom objects](custom-objects.md).
- important (default: `false`): If true, tries up to 100 positions for each object.
- randomRotation (default: `true`)
- minCount (default: `0`), maxCount (default: `10`): Amount of objects.
- minDistanceFromOtherObjects (default: `10`): Minimum distance between the event objects.
- maxDistance (default: `100`): Maximum distance from the event center. If 0, the event radius is used.
- maxSlope (default: `90`)
- water (default: `CanSpawnInWater`): Either `CanSpawnInWater`, `CanNotSpawnInWater` or `OnlySpawnInWater`.

## Example

```yaml
- name: MyEvent
  mapToken: My event
  minRadius: 150
  maxRadius: 150
  minDuration: 3600
  maxDuration: 7200
  biome: Meadows
  environment: Storm
  customChance: 50
  customInterval: 600
  startMessage: The sky darkens
  startCommands:
  - spawn Boar 5
  objects:
  - prefabs:
    - Pickable_Mushroom
    minCount: 5
    maxCount: 10
```
