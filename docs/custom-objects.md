# Custom objects

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

Initial object data in locations can be changed by using the `objectData` field. This affects both original, custom and [blueprint](blueprints.md) objects.

Data is merged from multiple layers and levels. The order for layers is:

1. Data from location.
2. Data from dungeon.
3. Data from room.
4. Data from blueprint or custom object (the highest priority).

Each layer has three levels:

1. `all` data applying to all objects.
2. Component specific data applying to all objects with the component.
3. Object specific data applying to specific objects.

The order matters if multiple data entries set the same field. Higher numbers are applied later, overriding any previous values.

For example you can override location specific data with room data, but not the other way around. Or if the blueprint has infinite health then it can't be changed by using the `objectData` field. But other data could be set like wear from Structure Tweaks mod.

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
