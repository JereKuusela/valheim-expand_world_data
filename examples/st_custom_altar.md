# Spawner Tweaks: Custom altar

Spawner Tweaks can be used to turn any object into a chest.

## First you need the data entry

1. `spawn GlowingMushroom`
2. `tweak_altar amount=-1 spawn=Wolf`
3. `ew_copy altar_wolf`
4. Open `expand_data.yaml` and remove "override_component":

```yaml
- name: altar_wolf
  ints:
  # Hash of Wolf.
  - override_spawn, 1010961914
  - override_amount, -1
```

## Then add the location

1. Open `expand_locations.yaml` and copy paste the existing `Eikthyrnir` entry.
2. Add `data: altar_wolf` to the entry.
3. `locations_add Eikthyrnir:Wolf start force` to spawn the location.
4. `find Eikthyrnir:Wolf` to find the location.

```yaml
# New location variant.
- prefab: Eikthyrnir:Wolf
  biome: Meadows
  biomeArea: median
  quantity: 3
  maxDistance: 0.1
  minAltitude: 1
  prioritized: true
  randomRotation: true
  maxTerrainDelta: 3
  forestTresholdMin: 1
  forestTresholdMax: 5
  exteriorRadius: 10
  clearArea: true
  # Add data.
  data: altar_wolf
```
