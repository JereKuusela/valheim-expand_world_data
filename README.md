# Expand World Data

Allows adding new biomes and changing most of the world generation.

Always back up your world before making any changes!

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

## Features

- Add new biomes.
- Change biome distribution.
- Change data like locations, vegetation and weather.
- Config sync to ensure all clients use the same settings.
- Change events with [Expand World Events](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Events/).
- Change factions with [Expand World Factions](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Factions/).
- Change prefabs with [Expand World Prefabs](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Prefabs/).
- Change spawns with [Expand World Spawns](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Spawns/).

For example you can create entirely flat worlds with only Meadows for building. Or group up colder biomes up north while more warmer biomes end up in the other side. Or just have a world with terrain shapes no one has ever seen before.

## Tutorials

- How to make custom biomes: <https://youtu.be/TgFhW0MtYyw> (33 minutes, created by StonedProphet)
- How to use blueprints as locations with custom spawners: <https://youtu.be/DXtm-WLF6KE> (30 minutes, created by StonedProphet)

## Configuration

Settings are automatically reloaded (consider using [Configuration manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)). This can lead to weird behavior so it's recommended to make a fresh world after you are done configuring.

Note: Pay extra attention when loading old worlds. Certain configurations can cause alter the terrain significantly and destroy your buildings.

Note: Old configuration from Expand World is automatically migrated to this mod.

### Server side

This mod can be used server only, without requiring clients to have it. However only following files can be configured:

- `expand_dungeons.yaml`: All fields.
- `expand_locations.yaml`: All fields, but some disabled locations won't work.
- `expand_rooms.yaml`: All fields.
- `expand_vegetation.yaml`: All fields.
- `expand_world.cfg`: Only setting Zone spawners.

When doing this, enable `Server only` on the config to remove version check.

## Data

This mod provides additional configuration files (.yaml) that can be used to change most world generation related data.

These files are generated automatically to the `config/expand_world` folder when loading a world (unless they already exist).

Each file can be disabled from the main .cfg file to improve compatibility and performance (less network usage and processing when joining the server).

Data can be split to multiple files. The files are loaded alphabetically in reverse order. For example `expand_biomes_custom.yaml` would be loaded first and then `expand_biomes.yaml`.

Note: Some files are server side only. Use single player for testing so that your client has access to all of the information. Mods that cause clients to regenerate dungeons or locations may not work correctly.

Note: Some files automatically add missing entries to add new content automatically. If you want to remove entries, set enabled to false instead. Automatic data migration can be disabled from the config.

Note: Editing files automatically reloads them. This can be disabled from the config (requires restart to take effect). If disabled, the world must be reloaded to load any changes.

### Blueprints

Dungeons, locations and vegetation support using blueprints to spawn multiple objects. Both PlanBuild .blueprint and BuildShare .vbuild files are supported (however PlanBuild files are preferred).

The file format is slightly modified from the usual:

- Two new fields are added to both .blueprint and .vbuild files.
  - zdoData initializes the object with a specific data. Infinity Hammer automatically saves this when creating .blueprint files.
  - chance is a number between 0 and 1. These must be added manually to the file.
  - .blueprint format: name;posX;posY;posZ;rotX;rotT;rotZ;rotW;info;scaleX;scaleY;scaleZ;zdoData;chance
  - .vbuild format: name;rotX;rotT;rotZ;rotW;posX;posY;posZ;zdoData;chance
- Blueprints can contain other blueprints as objects. These must added manually to the file.
- Center piece (bottom center of the blueprint) can be set to a certain object. This object is never spawned to the world.
  - Infinity Hammer saves this information to .blueprint files.
  - If the center piece is not found, the blueprint is centered automatically and placed 0.05 meters towards the ground.

## Biomes

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
- maximumAltitude (default: `1000` meters): Maximum altitude.
- minimumAltitude (default: `-1000` meters): Minimum altitude.
- excessFactor (default: `0.5`): How strongly the altitude is reduced if over the maximum or minimum limit. For example 0.5 square roots the excess altitude.
- paint: Default terrain paint. Format is `dirt,cultivated,paved,vegetation` (from 0.0 to 1.0) or a pre-defined color (cultivated, dirt, grass, grass_dark, patches, paved, paved_dark, paved_dirt, paved_moss)
- color: Terrain style. Not fully sure how this works but the color value somehow determines which default biome terrain style to use.
- mapColorMultiplier (default: `1.0`): Changes how quickly the terrain altitude affects the map color. Increasing the value can be useful for low altitude biomes to show the altitude differences better. Lowering the value can be useful for high altitude biomes to reduce amount of white color (from mountain altitudes). Negative value can be useful for underwater biomes to show the map color (normally all underwater areas get blueish color).
- mapColor: Color in the minimap (red, green, blue, alpha).
- musicMorning: Music override for the morning time.
- musicDay: Music override for the day time.
- musicEvening: Music override for the evening time.
- musicNight: Music override for the night time.
- noBuild (default: `false`): If true, players can't build in this biome.
- statusEffects: List of status effects that are active in this environment.
  - See [Status effects](#status-effects) for details.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.

## World

The file `expand_world.yaml` sets the biome distribution.

Each entry in the file adds a new rule. When determing the biome, the rules are checked one by one from the top until a valid rule is found. This means the order of entries is especially important for this file.

- biome: Identifier of the biome if this rule is valid.
- maxAltitude (default: `1000` meters): Maximum terrain height relative to the water level.
- minAltitude (default: `0` meters if maxAltitude is positive, otherwise `-1000` meters): Minimum terrain height relative to the water level.
- maxDistance (default: `1.0` of world radius): Maximum distance from the world center.
- minDistance (default: `0.0` of world radius): Minimum distance from the world center.
- minSector (default: `0.0` of world angle): Start of the [circle sector](https://en.wikipedia.org/wiki/Circular_sector).
- maxSector (default: `1.0` of world angle): End of the [circle sector](https://en.wikipedia.org/wiki/Circular_sector).
- centerX (default: `0.0` of world radius): Moves the center point away from the world center.
- centerY (default: `0.0` of world radius): Moves the center point away from the world center.
- amount (default: `1.0` of total area): How much of the valid area is randomly filled with this biome. Uses normal distribution, see values below.
- stretch (default: `1.0`): Same as the `Stretch biomes` setting but applied just to a single entry. Multiplies the size of biome areas (average total area stays the same).
- seed: Overrides the random outcome of `amount`. Numeric value fixes the outcome. Biome name uses a biome specific value derived from the world seed. No value uses biome from the `terrain` parameter.
- wiggleDistance (default: `true`): Applies "wiggle" to the `minDistance`.
- wiggleSector (default: `true`): Applies "wiggle" to the `maxSector` and `minSector`.

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

### Wiggle

"Wiggle" adds a sin wave pattern to the distance/sector borders for less artifical biome transitions. The strength can be globally configured in the main .cfg file.

## Environments

The file `expand_environments.yaml` sets the available weathers. Command `ew_musics` can be used to print available musics.

- name: Identifier to be used in other files.
- particles: Identifier of a default environment to set particles. Required for new environments.
- isDefault (default: `false`): The first default environment is loaded at the game start up. No need to set this true unless removing from the Clear environment.
- isWet (default: `false`): If true, is considered to be raining.
- isFreezing (default: `false`): If true, causes the freezing debuff.
- isFreezingAtNight (default: `false`): If true, causes the freezing at night.
- isCold (default: `false`): If true, causes the cold debuff.
- isColdAtNight (default: `false`): If true, causes the cold at night.
- alwaysDark (default: `false`): If true, causes constant darkness.
- windMin (default: `0.0`): The minimum wind strength.
- windMax (default: `1.0`): The maximum wind strength.
- rainCloudAlpha (default: `0.0`): ???.
- ambientVol (default: `0.3`): ???.
- ambientList: ???.
- musicMorning: Music override for the morning time. Higher priority than the biome value.
- musicDay: Music override for the day time. Higher priority than the biome value.
- musicEvening: Music override for the evening time. Higher priority than the biome value.
- musicNight: Music override for the night time. Higher priority than the biome value.
- ambColorDay, ambColorNight, sunColorMorning, sunColorDay, sunColorEvening, sunColorNight: Color values.
- fogColorMorning, fogColorDay, fogColorEvening, fogColorNight, fogColorSunMorning, fogColorSunDay, fogColorSunEvening, fogColorSunNight: Color values.
- fogDensityMorning, fogDensityDay, fogDensityEvening, fogDensityNight (default: `0.01`): ???.
- lightIntensityDay (default: `1.2`): ???.
- lightIntensityNight (default: `0`): ???.
- sunAngle (default: `60`): ???.
- statusEffects: List of status effects that are active in this environment.
  - See [Status effects](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Status_effects) for details.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.

Note: As you can see, lots of values have unknown meaning. Probably better to look at the existing environments for inspiration.

## Clutter

The file `expand_clutter.yaml` sets the small visual objects.

- prefab: Name of the clutter object.
- enabled (default: `true`): Quick way to disable this entry.
- amount (default: `80`): Amount of clutter.
- biome: List of possible biomes.
- instanced (default: `false`): Way of rendering or something. Might cause errors if changed.
- onUncleared (default: `true`): Only on uncleared terrain.
- onCleared (default: `false`): Only on cleared terrain.
- scaleMin (default: `1.0`): Minimum scale for instanced clutter.
- scaleMax (default: `1.0`): Maximum scale for instanced clutter.
- minTilt (default: `0` degrees): Minimum terrain angle.
- maxTilt (default: `10` degrees): Maximum terrain angle.
- minAltitude (default: `-1000` meters): Minimum terrain altitude.
- maxAltitude (default: `1000` meters): Maximum terrain altitude.
- minVegetation (default: `0`): Minimum vegetation mask.
- maxVegetation (default: `0`): Maximum vegetation mask.
- snapToWater (default: `false`): Placed at water level instead of terrain.
- terrainTilt (default: `false`): Rotates with the terrain angle.
- randomOffset (default: `0` meters): Moves the clutter randomly up/down.
- minOceanDepth (default: `0` meters): Minimum water depth (if different from max).
- maxOceanDepth  (default: `0` meters): Maximum water depth (if different from min).
- inForest (default: `false`): Only in forests.
- forestTresholdMin (default: `0`): Minimum forest value (if only in forests).
- forestTresholdMax (default: `0`): Maximum forest value (if only in forests).
- fractalScale  (default: `0`): Scale when calculating the fractal value.
- fractalOffset  (default: `0`): Offset when calculating the fractal value.
- fractalThresholdMin  (default: `0`): Minimum fractal value.
- fractalThresholdMax  (default: `1`): Maximum fractal value.

## Locations

The file `expand_locations.yaml` sets the available locations and their placement. This is a server side feature, clients don't have access to this data.

Note: Missing locations are automatically added to the file. To disable, set `enabled` to `false` instead of removing anything.

Note: Each zone (64m x 64m) can only have one size.

See the [wiki](https://valheim.fandom.com/wiki/Points_of_Interest_(POI)) for more info.

See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).

Locations are pregenerated at world generation. You must use `genloc` command to redistribute them on unexplored areas after making any changes. For already explored areas, you need to use Upgrade World mod.

- prefab: Identifier of the location object or name of blueprint file. Check wiki for available locations. Hidden ones work too. To create a clone of an existing location, add `:text` to the prefab. For example "Dolmen01:Ghost".
- enabled (default: `true`): Quick way to disable this entry.
- biome: List of possible biomes.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome).
- dungeon: Overrides the default dungeon generator with a custom one from `expand_dungeons.yaml`.
- quantity: Maximum amount. Actual amount is determined if enough suitable positions are found. The base .cfg has a setting to multiply these.
- minDistance (default: `0.0` of world radius): Minimum distance from the world center. Values over 2.0 are considered as meters.
- maxDistance (default: `1.0` of world radius): Maximum distance from the world center. Values over 2.0 are considered as meters.
- minAltitude (default: `0`): Minimum altitude.
- maxAltitude (default: `1000`): Maximum altitude.
- prioritized (default: `false`): Generated first with more attempts.
- centerFirst (default: `false`): Generating is attempted at world center, with gradually moving towards the world edge.
- unique (default: `false`): When placed, all other unplaced locations are removed. Guaranteed maximum of one instance.
- group: Group name for `minDistanceFromSimilar`.
- minDistanceFromSimilar (default: `0` meters): Minimum distance between the same location, or locations in the `group` if given.
- discoverLabel: Shown text when the location is discovered.
- iconAlways: Location icon that is always shown. Use `ew_icons` to see what is available.
  - Format is `icon,size,pulse`.
  - Size 5 or more is considered as meters. These icons scale up and down with the zoom level.
  - Putting anything on the third value causes the icon to pulse. This is not supported with meters.
- iconPlaced: Location icon to show when the location is generated. Use `ew_icons` to see what is available.
  - Format is `icon,size,pulse`.
  - Size 5 or more is considered as meters. These icons scale up and down with the zoom level.
  - Putting anything on the third value causes the icon to pulse. This is not supported with meters.
- randomRotation (default: `false`): Randomly rotates the location (unaffected by world seed).
- slopeRotation (default: `false`): Rotates based on the terrain angle. For example for locations at mountain sides.
- snapToWater (default: `false`): Placed at the water level instead of the terrain.
- minTerrainDelta (default: `0` meters): Minimum nearby terrain height difference.
- maxTerrainDelta (default: `10` meters): Maximum nearby terrain height difference.
- inForest (default: `false`): Only in forests.
- forestTresholdMin (default: `0`): Minimum forest value (if only in forests).
- forestTresholdMax (default: `0`): Maximum forest value (if only in forests).
- groundOffset (default: `0` meters): Placed above the ground.
- data: ZDO data override. For example to change altars with Spawner Tweaks mod (`object copy` from World Edit Commands).
- locationObjectSwap: Changes location objects to other objects.
- dungeonObjectSwap: Changes dungeon objects to other objects.
- objectSwap: Changes location and dungeon objects to other objects.
  - See [Object swaps](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Object_swaps) for details.
  - See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).
- locationObjectData: Replaces object data in the location.
- dungeonObjectData: Replaces object data in the dungeon.
- objectData: Replaces object data in the location and the dungeon.
  - See [Object data](https://github.com/JereKuusela/valheim-expand_world_data#Object_data) for details.
- objects: Extra objects in the location, relative to the location center.
  - See [Custom objects](https://github.com/JereKuusela/valheim-expand_world_data#Custom_objects) for details.
- randomDamage (default: `false`): If true, pieces without custom health are randomly damaged. If all, all pieces are randomly damaged.
- exteriorRadius: How many meters are cleared, leveled or no build. If not given for blueprints, this is the radius of the blueprint (+ 2 meters).
  - Note: Maximum suggested value is 32 meters. Higher values go past the zone border and can cause issues.
- commands: List of commands that will be executed when spawning the location.
  - Use `<x>`, `<y>` and `<z>` in the command to use the location center point.
  - Use `<a>` in the command to use the location rotation.
  - Basic arithmetic is supported. For example `<x>+10` would add 10 meters to the x coordinate.
- clearArea (default: `false`): If true, vegetation is not placed within `exteriorRadius`.
- noBuild (default: `false`): If true, players can't build within `exteriorRadius`. If number, player can't build within the given radius.
- noBuildDungeon (default: `false`): If true, players can't build inside dungeons within the whole zone. If number, player can't build inside dungeons within the given radius.
  - Note: For bigger dungeons, the number should be set manually (dungeon `bounds` divided by sqrt 2).
- levelArea (default: `true` for blueprints): Flattens the area.
- levelRadius (default: half of `exteriorRadius`): Size of the leveled area.
- levelBorder (default: half of `exteriorRadius`): Adds a smooth transition around the `levelRadius`.
- paint: Paints the terrain. Format is `dirt,cultivated,paved,vegetation` (from 0.0 to 1.0) or a pre-defined color (cultivated, dirt, grass, grass_dark, patches, paved, paved_dark, paved_dirt, paved_moss).
- paintRadius (default: `exteriorRadius`): Size of the painted area.
- paintBorder (default: `5`): Adds a smooth transition around the `paintRadius`.
- scaleMin (default: `1`): Minimum scale. Number or x,z,y (with y being the height).
  - Note: Each object is scaled independently. Distances between objects are not scaled.
  - Note: All objects can't be scaled without using Structure Tweaks mod.
- scaleMax (default: `1`): Maximum scale. Number or x,z,y (with y being the height).
- scaleUniform (default: `true`): If disabled, each axis is scaled independently.

## Dungeons

The file `expand_dungeons.yaml` sets dungeon generators. This is a server side feature, clients don't have access to this data.

Command `ew_dungeons` can be used to list available rooms for each dungeon.

- name: Name of the dungeon generator.
- algorithm: Type of the dungeon. Possible values are `Dungeon`, `CampGrid` or `CampRadial`.
- bounds (default: `64`): Maximum size of the dungeon in meters. Format is `x,z,y` or a single number for all directions.
  - Reasonable maximum is 3 zones which is 192 meters.
  - Note: Zone size is 64m x 64m. So values above that causes overflow to the adjacent zones.
  - Note: Dungeons have an environment cube that has 64 meter size. This is automatically scaled, unless running in the server side only mode.
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
- interiorTransform (default: `false`): Some locations may require this being true. If you notice weird warnings, try setting this to true.
- objectData: Replaces object data in the dungeon.
  - See [Object data](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Object_data) for details.
- objectSwap: Changes dungeon objects to other objects.
  - See [Object swaps](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Object_swaps) for details.
  - Note: If a room has object swaps, the dungeon swaps are applied first.

## Rooms

The file `expand_rooms.yaml` sets available dungeon rooms. This is a server side feature, clients don't have access to this data.

See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).

New rooms can be created from blueprints or cloning an existing room by adding `:suffix` to the name.

- name: Name of the room prefab.
- theme: Determines in which dungeons this room can appear. Custom values can be used.
- enabled (default `true`): Quick way to disable this room.
- entrance (default `false`): If true, this room is used only as the first room.
  - At least one entrance room is required. If multiple exist, one is randomly selected (`weight` field is never used).
  - One of the connections must be set as `entrance`. Even if multiple exist, the first one is used.
- endCap (default `false`): If true, this room is used to seal open ends at end of the generation.
  - These rooms should only have one connection, so that no open ends are left.
  - Each connection type should have a corresponding end cap, so that each connection can be sealed.
- divider (default `false`): If true, this room is used to seal mismatching connections.
  - The generator can create cycles so that two open connections are in the same position.
    - If the connection types are the same, nothing is done.
    - However if the types are different, a divider room is used to seal the connection.
  - These rooms should only have one connection, so that no open ends are left. The connection type doesn't matter.
  - These rooms should be very small (probably just a single wall).
- endCapPriority (default `0`): Rooms with a higher priority are attempted first, unless `roomWeights` is enabled.
- minPlaceOrder (default `0`): Minimum amount of rooms between this room and the entrance.
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
- objects: Extra objects in the room.
  - See [Custom objects](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Custom_objects) for details.
- objectSwap: Changes room objects to other objects.
  - See [Object swaps](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md#Object_swaps) for details.
  - Note: If the dungeon has object swaps, those are applied first.

## Vegetation

The file `expand_vegetations.yaml` sets the generated objects. This is a server side feature, clients don't have access to this data.

Changes only apply to unexplored areas. Upgrade World mod can be used to reset areas.

Note: Missing vegetation are automatically added to the file. To disable, set `enabled` to `false` instead of removing anything.

- prefab: Identifier of the object or name of blueprint file.
- enabled (default: `true`): Quick way to disable this entry.
- min (default: `1`): Minimum amount (of groups) to be placed per zone (64m x 64m).
- max (default: `1`): Maximum amount (of groups) to be placed per zone (64m x 64m). If less than 1, has only a chance to appear.
- forcePlacement (default: `false`): By default, only one attempt is made to find a suitable position for each vegetation. If enabled, 50 attempts are done for each vegetation.
- scaleMin (default: `1`): Minimum scale. Number or x,z,y (with y being the height).  
- scaleMax (default: `1`): Maximum scale. Number or x,z,y (with y being the height).
- scaleUniform (default: `true`): If disabled, each axis is scaled independently.
- randTilt (default: `0` degrees): Random rotation within the degrees.
- chanceToUseGroundTilt (default: `0.0`): Chance to set rotation based on terrain angle (from 0.0 to 1.0).
- biome: List of possible biomes.
- biomeArea: List of possible biome areas (edge = zones with multiple biomes, median = zones with only a single biome, 4 = unused from Valheim data).
- blockCheck (default: `true`): If enabled, clear ground is required.
- minAltitude (default: `0` meters): Minimum terrain altitude.
- maxAltitude (default: `1000` meters): Maximum terrain altitude.
- minOceanDepth (default: `0` meters): Minimum ocean depth (interpolated from zone corners so slightly different from `minAltitude`).
- maxOceanDepth (default: `0` meters): Maximum ocean depth (interpolated from zone corners so slightly different from `maxAltitude`).
- minVegetation (default: `0`): Minimum vegetation mask (random value from 0.0 to 1.0, only used in Mistlands biome).
- maxVegetation (default: `0`): Maximum vegetation mask (random value from 0.0 to 1.0, only used in Mistlands biome).
- minTilt (default: `0` degrees): Minimum terrain angle.
- maxTilt (default: `90` degrees): Maximum terrain angle.
- terrainDeltaRadius (default: `0` meters): Radius for terrain delta limits.
  - 10 random points are selected within this radius.
  - The altitude difference between the lowest and highest point must be within the limits.
- minTerrainDelta (default: `0` meters): Minimum terrain height change.
  - Higher values cause the vegetation to be based on slopes.
- maxTerrainDelta (default: `10` meters): Maximum terrain height change.
  - Lower values cause the vegetation to be based on flatter areas.
- snapToWater (default: `false`): If enabled, placed at the water level instead of the terrain level.
- snapToStaticSolid (default: `false`): If enabled, placed at the top of solid objects instead of terrain.
- groundOffset (default: `0` meters): Placed above the ground.
- groupSizeMin (default: `1`): Minimum amount to be placed per group.
- groupSizeMax (default: `1`): Maximum amount to be placed per group.
- groupRadius (default: `0` meters): Radius for group placement. This should be less than 32 meters to avoid overflowing to adjacent zones.
- inForest (default: `false`): If enabled, forest thresholds are checked.
  - This creates clusters of vegetation, instead of them being placed evenly.
  - Thresholds between 0 and 1.15 are shown as forest in the minimap.
  - Smaller values would be more closer to the forest center.
  - Larger values would be more away from the forest center.
- forestTresholdMin (default: `0`): Minimum forest value.
- forestTresholdMax (default: `0`): Maximum forest value.
- clearRadius (default: `0` meters): Extra distance away from location clear areas.
  - Note: By default, only the vegetation center point is checked. So big objects can have part of them inside clear areas.
  - If this is a problem, increase the value.
- clearArea (default: `false`): If set, this vegetation creates a clear area similar to locations (based on `clearRadius`).
  - When using this, it's important to put the vegetation at top of the file so that it's generated before other vegetation.
  - Unlike locations, the clear area can't cross zone borders. If the vegetation spawns near the border, it may have a smaller clear area.
  - Use `groupRadius` to move the vegetation away from the zone borders.
- requiredGlobalKey: List of [global keys](https://valheim.fandom.com/wiki/Global_Keys). If all are set, the vegetation is placed.
  - Note: This doesn't affect already generated zones. Intended to be used with Upgrade World + Cron Job mods.
- forbiddenGlobalKey: List of [global keys](https://valheim.fandom.com/wiki/Global_Keys). If any is set, the vegetation is not placed.
  - Note: This doesn't affect already generated zones. Intended to be used with Upgrade World + Cron Job mods.
- data: ZDO data override. For example to create hidden stashes with Spawner Tweaks mod (`object copy` from World Edit Commands).

## Custom objects

New objects can be added to locations, dungeon rooms and zone spawners (creature spawns) by using the `objects` field.

The objects are added relative to the spawn or location center.

```yaml
  - objects:
    # Format is:
    - id, posX,posZ,posY, rotY,rotX,rotZ, scaleX,scaleZ,scaleY, chance, data
    # Adds a mushroom at the center to visually show where it is.
    - GlowingMushroom, 0,0,0
    # Adds a mushroom 20 meters away from the center that snaps to the ground.
    - GlowingMushroom, 20,0,snap
    # Adds a bigger mushroom if the first one wasn't big enough.
    - GlowingMushroom, 0,0,0 0,0,0 2,2,2
    # Adds a chest near the center with 90 degrees rotation that has a 50% chance to appear.
    - Chest, 5,0,0, 90,0,0, 1,1,1, 0.5
    # Adds a chest with specific data.
    # It's recommended to use the objectData field if possible (less typing).
    # However this can be used to override the objectData.
    - Chest, 5,0,0, 90,0,0, 1,1,1, 0.5, infinite_health
```

For the default objects, you can use commands `ew_locations` and `ew_rooms` to print location or room contents.

## Object swaps

Objects in locations can be swapped to other objects by using the `objectSwap` field. This affects both original and custom objects.

- expand_locations.yaml: Affects only the overworld part of the location.
- expand_dungeons.yaml: Affects all rooms in the dungeon.
- expand_rooms.yaml: Affects only the single room. Dungeon swap is applied first.
  - If the dungeon swaps object A to object X, then the room must swap the object X (not the object A).
  - If the there is no dungeon swap, then the room must swap the object A.
  - If you need to handle both situations, add swap for both A and X objects.

Note: To prevent a custom object being swapped, use a dummy object and then create a swap for it. For example a custom object A would get swapped to object X, then use object D instead and swap it back to the object A.

Note: Objects can be removed by swapping to nothing.

```yaml
  - objectSwap:
      # Swaps object A to object X.
      - idA, idX
      # Adds another swap for object A. The swap is randomly selected.
      # Total weight: 1 + 2 = 3.
      # 2 / 3 =  66% chance to select this swap.
      - idA:2, idY
      # Same as above but in a single line.
      - idB, idX, idY:2
      # Dummy swap. To add a custom object A, use object D instead and swap it back to A.
      - idD, idA
      # Swap object E to nothing.
      - idE,
      # Swap object F to nothing or object X (50% chance).
      - idF,,X
```

## Object data

Initial object data in locations can be changed by using the `objectData` field. This affects both original, custom and blueprint objects.

Data is merged from multiple levels. The order is:

1. `all` data from `expand_locations.yaml` or `expand_vegetations.yaml`.
2. Object specific data from `expand_locations.yaml` or `expand_vegetations.yaml`.
3. `all` data from `expand_dungeons.yaml`.
4. Object specific data from `expand_dungeons.yaml`.
5. `all` data from `expand_rooms.yaml`.
6. Object specific data from `expand_rooms.yaml`.
7. Blueprint or custom object data (the highest priority).

For example if the blueprint has infinite health then it can't be changed by using the `objectData` field. But other data could be set like wear from Structure Tweaks mod.

There are two ways to set data:

1. Add a new entry to `data.yaml` with `data save` and use its name.
2. Use `data copy_raw` to copy the raw data value.

See [data documentation](https://github.com/JereKuusela/valheim-world_edit_commands/blob/main/README_data.md) for more info.

```yaml
  - objectData:
      # Sets all objects data to infinite_health.
      - all, infinite_health
      # Overrides idA health to default_health.
      - idA, default_health
      # Adds another possible object data for idA.
      # Total weight: 1 + 2 = 3.
      # 2 / 3 = 66% chance for infinite_health and 33% chance for default_health.
      - idA:2, infinite_health
      # Same for idB but in a single line.
      - idB, default_health, infinite_health:2
```

## Status effects

Both the biome and the current environment can apply status effects to the player. Effects can be active during the day, night or both.

Each entry has following fields:

- `name`: Name of the effect.
- `requiredGlobalKeys`: Active if all of these world keys are set.
- `forbiddenGlobalKeys`: Active if none of these world keys are set.
- `requiredPlayerKeys`: Active if all of these player keys are set.
- `forbiddenPlayerKeys`: Active if none of these player keys are set.
- `day`: Active during the day.
- `night`: Active during the night.
- `duration`: Duration in seconds. 0 is "permanent". If not given, uses the default duration of the status effect.
  - Duration is not used for damaging effects (Burning, Poison and Spirit).
  - Effects start ticking down when leaving the biome or environment. Effects with "permanent" duration are instantly removed.
- `damage`: Damage that is affected by both armor and resistances.
  - Burning: Damage per second. Duration is always 5 seconds.
  - Spirit: Damage per second. Duration is always 3 seconds.
  - Poison: Damage over the duration (duration scales with the damage).
- `damageIgnoreArmor`: Damage that ignores armor.
- `damageIgnoreAll`: Damage that ignores armor and resistances.
- `immuneWithResist`: If true, damage resistance counts as immunity.
  - Note: `damageIgnoreAll` is not affected.

See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).
