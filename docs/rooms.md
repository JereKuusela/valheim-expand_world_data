# Rooms

Rooms are the building blocks of dungeons.

## Configuration

The file `expand_rooms.yaml` is created automatically when loading a world. Enable or disable rooms from the config using the `Room data` setting. This is a server side feature, clients don't have access to this data.

New rooms can be created from [blueprints](blueprints.md) or cloning an existing room by adding `:suffix` to the name.

See [examples](../examples/examples.md).

### Room Fields

- name: Name of the room prefab.
- theme: Determines in which dungeons this room can appear. Custom values can be used.
- enabled (default: `true`): Quick way to disable this room.
- entrance (default: `false`): If true, this room is used only as the first room.
  - At least one entrance room is required. If multiple exist, one is randomly selected (`weight` field is never used).
  - One of the connections must be set as `entrance`. Even if multiple exist, the first one is used.
- endCap (default: `false`): If true, this room is used to seal open ends at end of the generation.
  - These rooms should only have one connection, so that no open ends are left.
  - Each connection type should have a corresponding end cap, so that each connection can be sealed.
- divider (default: `false`): If true, this room is used to seal mismatching connections.
  - The generator can create cycles so that two open connections are in the same position.
    - If the connection types are the same, nothing is done.
    - However if the types are different, a divider room is used to seal the connection.
  - These rooms should only have one connection, so that no open ends are left. The connection type doesn't matter.
  - These rooms should be very small (probably just a single wall).
- endCapPriority (default: `0`): Rooms with a higher priority are attempted first, unless `roomWeights` is enabled.
- minPlaceOrder (default: `0`): Minimum amount of rooms between this room and the entrance.
- weight (default: `1`): Chance of this room being selected (relative to other weights), unless `roomWeights` is disabled.
- faceCenter (default: `false`): If true, the room is always rotated towards the camp center. If false, the room is randomly rotated.
- perimeter (default: `false`): If true, this room is placed on the camp perimeter (edge).
- size: Format `x,z,y`. Size of this room in meters. Decimals are also supported.
  - Probably no reason to change this for existing rooms.
  - For blueprints, this is automatically calculated but recommended to be set manually.
  - Collision check removes 0.1 meters from the size which may cause some overlap.
- connections: List of doorways.
  - position: Format `posX,posZ,posY,rotY,rotX,rotZ` or `id` for blueprints. Position relative to the room.
    - If missing, the base room position is used.
    - For blueprints, this must be set. The easiest way is to mark the position with an object and use the `id`.
  - type: Type of the connection. Only connections with the same type are connected.
  - entrance (default: `false`): If true, used for the entrance.
  - door (default: `true`): If true, allows placing door. If `other`, allows placing door if the other connection also allows placing a door.
  - exitOnly (default: `false`): If true, this connection is only used for already placed rooms. This can be used to force room direction.
- objects: Extra objects in the room.
  - See [Custom objects](custom-objects.md) for details.
- objectSwap: Changes room objects to other objects.
  - See [Object swaps](custom-objects.md#object-swaps) for details.
  - Note: If the dungeon has object swaps, those are applied first.

## Examples

New room created by cloning an existing room (adding `:suffix` to the name). The `theme` determines which dungeons can use the room:

```yaml
- name: forestcrypt_room_16:Rare
  theme: ForestCrypt
  weight: 0.2
  objects:
    - GlowingMushroom, 0,0,0
```

Changing the weight of an existing room:

```yaml
- name: forestcrypt_room_16
  weight: 3
```

Disabling an existing room:

```yaml
- name: forestcrypt_Chasm01
  enabled: false
```
