# Events

Events are special encounters that can occur in the world.

## Configuration

The file `expand_events.yaml` is created automatically when loading a world. Enable or disable events from the config using the `Event data` setting.

See the [wiki](https://valheim.wiki/Events) for more info about vanilla events.

### Settings

All settings are server-side:

- Event data: Enable/disable the event system (default: `false` unless legacy mod detected).
- Multiple events: If enabled, multiple events can be active at the same time (default: `false`).
  - When a new event starts, the previous one stays active (unless too close).
  - Clients receive the closest event.
  - Active events are not saved to the save file. Restarting the server removes active events.
- Minimum distance between events: When multiple events are enabled, new events cancel previous events within this distance (default: `100` meters).
- Check per player: If enabled, the event check is done separately for each player (default: `false`).
  - This makes events much more likely to happen.
- Random event chance: The chance to try starting a random event (default: `20` percent).
- Random event interval: How often the game tries to start a random event (default: `46` minutes).

### Event Fields

Client side fields:

- name: Identifier. Multiple events can have the same name, allowing multiple configurations.
- enabled (default: `true`): Quick way to disable this entry.
- spawns: List of spawned objects during the event. See [Spawns](spawns.md) for more info.
- startMessage: Message shown on the screen when the event starts.
- endMessage: Message shown on the screen when the event ends.
- forceMusic: Event music to play.
- forceEnvironment: Event environment/weather to use.
- radius (default: `96` meters): Event area radius.
- spawnerDelay (default: `0` seconds): Delay before the event starts spawning.
- requiredPlayerKeys: Event becomes available if the player has any of these keys.
- requiredPlayerKeysAll: Event becomes available if the player has all of these keys.
- notRequiredPlayerKeys: Event is not available if the player has any of these keys.
- requiredKnownItems: Event becomes available if the player knows any of these items.
- notRequiredKnownItems: Event is not available if the player knows any of these items.

Server side fields:

- duration (default: `60` seconds): How long the event lasts.
- nearBaseOnly (default: `true`): Minimum amount of player base structures within 40 meters.
  - Value `true` is 3 structures, so the event can only trigger with at least 3 nearby structures.
  - Value `false` is 0 structures.
- **outsideBaseOnly** (default: `false`): Maximum amount of player base structures within 40 meters.
  - Value `true` is 2 structures, so the event can only trigger with up to 2 nearby structures.
  - Using this automatically disables **nearBaseOnly**.
- **pauseIfNoPlayerInArea** (default: `true`): The event timer pauses if no player in the area.
- **biome**: List of required biomes (default: any biome).
- **random** (default: `true`): The event can happen randomly (unlike boss events which happen when near a boss).
- **requiredEnvironments**: List of valid environments/weathers.
- **requiredGlobalKeys**: Event becomes available if the world has any of these keys.
- **notRequiredGlobalKeys**: Event is not available if the world has any of these keys.
- **playerLimit**: Amount of required players in the area (`min-max`). Used to trigger stronger events with more players.
- **playerDistance** (default: `100` meters): Distance from the triggering player.
- **eventLimit**: Amount of required events in the area (`min-max`). Requires **Multiple events** to be enabled.
- **customInterval**: Event specific interval. If set, a separate timer is used for this event.
- **customChance**: Event specific chance override.
- **startCommands**: Commands to execute when the event starts.
- **endCommands**: Commands to execute when the event ends.

### Example

```yaml
- name: greydwarf_swarm
  enabled: true
  spawns:
    - prefab: Greydwarf
      name: greydwarf
      maxSpawned: 4
      spawnInterval: 30
  startMessage: "A swarm of greydwarves approaches!"
  endMessage: "The greydwarves have left."
  duration: 300
  radius: 100
  requiredGlobalKeys: defeated_greydwarf
  startCommands:
    - say "Greydwarf swarm incoming!"
  endCommands:
    - say "The swarm has been defeated!"
```
