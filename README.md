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
- Some examples in the [examples](examples/examples.md).
- More examples and help in [Discord](https://discord.gg/VFRJcPwUdm).

## Configuration

Settings are automatically reloaded (consider using [Configuration manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)). This can lead to weird behavior so it's recommended to make a fresh world after you are done configuring.

Note: Pay extra attention when loading old worlds. Certain configurations can cause alter the terrain significantly and destroy your buildings.

### Server side

This mod can be used server only, without requiring clients to have it. However only following files can be configured:

- `expand_dungeons.yaml`: All fields.
- `expand_locations.yaml`: All fields.
- `expand_rooms.yaml`: All fields.
- `expand_vegetation.yaml`: All fields.
- `expand_world.cfg`: Only setting Zone spawners or Random locations.

When doing this, enable `Server only` on the config to remove version check.

### Ashlands & Deep North

Ashlands and Deep North have special terrain features that can be modified in the config.

Terrain is lowered before Ashland and Deep North to make Ocean appear. This can be disabled with settings "Ashlands gap" and "Deep North gap".

Ashlands terrain is limited by the position. This can be fully disabled with setting ""Restrict Ashlands position" or modified with settings "Ashlands length restriction" and "Ashlands width restriction".

Note: The same settings exist in Expand World Size mod. Only change these settings in one of the mods.

## Data

This mod provides additional configuration files (.yaml) that can be used to change most world generation related data.

These files are generated automatically to the `config/expand_world` folder when loading a world (unless they already exist).

Each file can be disabled from the main .cfg file to improve compatibility and performance (less network usage and processing when joining the server).

Data can be split to multiple files. The files are loaded alphabetically in reverse order. For example `expand_biomes_custom.yaml` would be loaded first and then `expand_biomes.yaml`.

Note: Some files are server side only. Use single player for testing so that your client has access to all of the information. Mods that cause clients to regenerate dungeons or locations may not work correctly.

Note: Some files automatically add missing entries to add new content automatically. If you want to remove entries, set enabled to false instead. Automatic data migration can be disabled from the config.

Note: Editing files automatically reloads them. This can be disabled from the config (requires restart to take effect). If disabled, the world must be reloaded to load any changes.

### Biomes

The file `expand_biomes.yaml` sets available biomes and their configuration.

See [Biomes](docs/biomes.md) for more info.

### Territories

The file `expand_territories.yaml` sets available territories and their configuration.

See [Territories](docs/territories.md) for more info.

### World

The file `expand_world.yaml` sets the biome distribution.

Each entry in the file adds a new rule. When determing the biome, the rules are checked one by one from the top until a valid rule is found. This means the order of entries is especially important for this file.

See [World](docs/world.md) for more info.

### Environments

The file `expand_environments.yaml` sets the available weathers.

Command `ew_musics` can be used to print available musics.

See [Environments](docs/environments.md) for more info.

### Clutter

The file `expand_clutter.yaml` sets the small visual objects.

See [Clutter](docs/clutter.md) for more info.

### Locations

The file `expand_locations.yaml` sets the available locations and their placement. This is a server side feature, clients don't have access to this data.

Locations are pregenerated at world generation. You must use `genloc` command to redistribute them on unexplored areas after making any changes. For already explored areas, you need to use Upgrade World mod.

See the [wiki](https://valheim.fandom.com/wiki/Points_of_Interest_(POI)) for more info.

See [Locations](docs/locations.md) for more info.

### Dungeons

The file `expand_dungeons.yaml` sets dungeon generators. This is a server side feature, clients don't have access to this data.

Command `ew_dungeons` can be used to list available rooms for each dungeon.

See [Dungeons](docs/dungeons.md) for more info.

### Rooms

Dungeon room configuration is documented in [Rooms](docs/rooms.md).

### Vegetation

The file `expand_vegetations.yaml` sets the generated objects. This is a server side feature, clients don't have access to this data.

Changes only apply to unexplored areas. Upgrade World mod can be used to reset areas.

See [Vegetation](docs/vegetation.md) for more info.

### Custom objects

New objects can be added to locations, dungeon rooms and zone spawners (creature spawns) by using the `objects` field.

See [Custom objects](docs/custom-objects.md) for more info.

### Status effects

Both the biome and the current environment can apply status effects to the player. Effects can be active during the day, night or both.

See [Status effects](docs/status-effects.md) for more info.
