# Locations: Custom chest

Check first [Locations: Variants](./locations_variants.md) to learn how to edit locations.

## Extra chest

```yaml
- prefab: StoneTowerRuins03
  objects:
  # Adds a chest at position X,Z,Y (try different values)
   - TreasureChest_meadows, X,Z,Y
```

## Chest that randomly appears

```yaml
- prefab: StoneTowerRuins03
  objects:
  # 50% chance to appear.  
  - TreasureChest_meadows, X,Z,Y, 0,0,0, 1,1,1, 0.5
```

## Chest with random content

```yaml
- prefab: StoneTowerRuins03
  objects:
  - TreasureChest_meadows, X,Z,Y, 0,0,0, 1,1,1, 0.5
  objectSwap:
  # 1/5 = 20% chance to replace the chest with a buried chest.
   - TreasureChest_meadows, TreasureChest_meadows:4, TreasureChest_meadows_buried
```

## Chest with custom content

1. `spawn TreasureChest_meadows`
2. Manually replace items items in the chest.
3. `ew_copy chest_resin`
4. Open `expand_data.yaml` and remove extra data from the copied data.
5. Repeat for other chests.

```yaml
- name: chest_resin
  strings:
  # Single resin.
  - items, aAAAAAEAAAAFUmVzaW4BAAAAAADIQgAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAA==
- name: chest_loot
  strings:
  # Other loot.
  - items, aAAAAAMAAAAFUmVzaW4BAAAAAADIQgAAAAAAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAVGbGludAQAAAAAAMhCAQAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAABUNvaW5zDAAAAAAAyEICAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAA=
 ```

```yaml
- prefab: StoneTowerRuins03
  objects:
  - TreasureChest_meadows, X,Z,Y, 0,0,0, 1,1,1, 0.5
  objectSwap:
  # 1/5 = 20% chance to replace the chest with a buried chest.
   - TreasureChest_meadows, TreasureChest_meadows:4, TreasureChest_meadows_buried
  objectData:
  # 90% chance for resin, 10% chance for better loot.
  - TreasureChest_meadows, chest_resin:9, chest_loot:1
  # 20% chance for resin, 80% chance for better loot.
  - TreasureChest_meadows_buried, chest_resin:1, chest_loot:4
```

## Spawner Tweaks

Spawners Tweaks mod can be used to create custom chests with loot tables.

See [Spawner Tweaks: Hidden chest](./st_hidden_chest.md) for more information.
