# Dungeons

The file `expand_dungeons.yaml` sets dungeon generators. This is a server side feature, clients don't have access to this data.

Command `ew_dungeons` can be used to list available rooms for each dungeon.

- name: Name of the dungeon generator.
- algorithm: Type of the dungeon. Possible values are `Dungeon`, `CampGrid` or `CampRadial`.
- bounds (default: `64`): Maximum size of the dungeon in meters. Format is `x,z,y` or a single number for all directions.
  - Reasonable maximum is 3 zones which is 192 meters.
  - Note: Zone size is 64m x 64m. So values above that causes overflow to the adjacent zones.
  - Note: Dungeons have an environment cube that has 64 meter size. This is automatically scaled, unless running in the server side only mode.
- randomSeed (default: `false`): If true, the generation result is always different instead of depending on the dungeon coordinates.
- themes: List of available room sets separated by ",".
  - For example `SunkenCrypt,ForestCrypt` would use both sets.
- maxRooms (default: `1`): Maximum amount of rooms. Only for Dungeon and CampRadial.
- minRooms (default: `1`): Minimum amount of rooms. Only for Dungeon and CampRadial.
- minRequiredRooms (default: `1`): Minimum amount of rooms in the required list. Only for Dungeon and CampRadial.
- requiredRooms: List of required rooms. Generator stops after required rooms and minimum amount of rooms are placed. Use command `ew_rooms` to print list of rooms.
- excludedRooms: List of rooms removed from the available rooms.
- roomWeights (default: `false`): Changes how rooms are randomly selected. Only for Dungeon.
  - If false, every room has the same chance to be selected (`weight` field is ignored).
  - If false, end cap is selected based on the highest `endCapPriority` field (`weight` field is not used).
- doorChance (default: `0`): Chance for a door to be placed. Only for Dungeon.
- doorTypes: List of possible doors. Each door has the same chance of being selected.
  - prefab: Identifier of the door object.
  - connectionType: Type of the door connection.
  - chance (default: `0`): Chance to be spawned if this door is selected. IF zero, the general `doorChance` is used instead.
- maxTilt (default: `90` degrees): Maximum terrain angle. Only for CampGrid and CampRadial.
- perimeterSections (default: `0`): Amount of perimeter walls to spawn. Only for CampRadial.
- perimeterBuffer (default: `0` meters): Size of the perimeter area around the camp. Only for CampRadial.
- campRadiusMin (default: `0` meters): Minimum radius of the camp. Only for CampRadial.
- campRadiusMax (default: `0` meters): Maximum radius of the camp. Only for CampRadial.
- minAltitude (default: `0` meters): Minimum altitude for the room. Only for CampRadial.
- gridSize (default: `0`): Size of the grid. Only for CampGrid.
- tileWidth (default: `0` meters): Size of a single tile. Only for CampGrid.
- spawnChance (default: `1`): Chance for each tile to spawn. Only for CampGrid.
- interiorTransform (default: `false`): Makes the dungeon to be generated at specific position instead of straight above the entrance.
  - This gives more room to fill out the entire zone.
  - The position is determined by the location entrance, so this only works if the location supports it.
  - Currently there is no need to change this setting, but you may need to change this if you change the entrance location.
- objectData: Replaces object data in the dungeon.
  - See [Object data](object-data.md) for details.
- objectSwap: Changes dungeon objects to other objects.
  - See [Object swaps](object-swaps.md) for details.
  - Note: If a room has object swaps, the dungeon swaps are applied first.
