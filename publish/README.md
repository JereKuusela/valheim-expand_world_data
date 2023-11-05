# Expand World Data

Allows adding new biomes and changing most of the world generation.

Always back up your world before making any changes!

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

Some features are available also as server only mode.

## Usage

See [documentation](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/README.md).

See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).

[<img width="80px" style="margin-bottom: -4" src="https://cdn.prod.website-files.com/6257adef93867e50d84d30e2/636e0b5493894cf60b300587_full_logo_white_RGB.svg">](https://discord.gg/VFRJcPwUdm) for help and examples.

## Migration for Hildir update

New dungeons, environments, events and locations are added automatically. Changes to existing dungeon rooms require manual changes.

### Automatic changes

- `expand_locations.yaml` should automatically add DevSpawnTest, Hildir_cave, Hildir_crypt and Hildir_plainsfortress.
  - Expand World Data version 1.7 did not add these correctly.
  - Check that Hildir_plainsfortress has `noBuild: 8.75`
- `expand_environments.yaml` should automatically add CavesHildir and CryptHildir.
- `expand_events.yaml` should automatically add hildirboss1, hildirboss2 and hildirboss3.
- `expand_dungeons.yaml` should automatically add DG_Hildir_Cave, DG_Hildir_ForestCrypt and DG_Hildir_PlainsFortress.
  - Expand World Data version 1.5 did not add these correctly.
  - Search and replace:
    - 1024 to CaveHildir
    - 2048 to ForestCryptHildir
    - 4096 to PlainsFortHildir
- `expand_rooms.yaml` should automatically add new rooms (for example plainsfortress_Hildir_Floor0).
  - Expand World Data version 1.5 did not add these correctly.
  - Search and replace:
    - 1024 to CaveHildir
    - 2048 to ForestCryptHildir
    - 4096 to PlainsFortHildir

### Manual changes

- `expand_rooms.yaml` is missing changes to the old rooms.
  - If you have changed the file, copy it to another folder.
  - Delete the `expand_rooms.yaml` so that it regenerates when loading a world.
  - Use any tool to compare the files. Update room themes as needed.

## Tutorials

- How to make custom biomes: <https://youtu.be/TgFhW0MtYyw> (33 minutes, created by StonedProphet)
- How to use blueprints as locations with custom spawners: <https://youtu.be/DXtm-WLF6KE> (30 minutes, created by StonedProphet)

## Credits

Thanks for Azumatt for creating the mod icon!

Thanks for blaxxun for creating the server sync!

Sources: [GitHub](https://github.com/JereKuusela/valheim-expand_world_data)
Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
