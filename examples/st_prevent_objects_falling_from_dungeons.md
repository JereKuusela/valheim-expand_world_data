# Structure Tweaks: Prevent objects falling from dungeons

Some objects have their "snap" setting set to terrain. This causes them to fall down to the ground when placed on a dungeon floor.

Structure Tweaks allows changing this setting when installed on all clients.

## First you need the data entry

Add to `expand_data.yaml`:

```yaml
# Object falls to the ground, through any object.
- name: terrain_fall
  ints:
  - override_fall, 1
# Objects falls until it hits any object.
- name: solid_fall
  ints:
  - override_fall, 2
# Object doesn't fall at all.
- name: no_fall
  ints:
  - override_fall, 3
```

## Then modify the location

Edit `expand_locations.yaml`:

```yaml
- prefab: TrollCave02
  objectSwap:
  # Converts to random mushroom.
  - Pickable_Mushroom_yellow, Pickable_Mushroom, Pickable_Mushroom_blue, Pickable_Mushroom_yellow
  objectData:
  # Adds no fall to every mushroom.
  - Pickable_Mushroom, no_fall
  - Pickable_Mushroom_blue, no_fall
  - Pickable_Mushroom_yellow, no_fall
```
