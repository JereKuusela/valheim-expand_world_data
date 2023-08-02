# Spawner Tweaks: Hidden chest

Spawner Tweaks can be used to turn any object into a chest.

First you need the data entry:
1. `spawn stubbe`
2. `tweak_chest maxamount=2 item=Torch item=Coins,1,10,20`
3. `ew_copy chest_stubbe`
4. Open `expand_data.yaml` and remove "InUse" and "addedDefaultItems":
```yaml
- name: chest_stubbe
  ints:
  - override_maximum_amount, 2
  strings:
  - override_component, chest
  - override_items, Torch|Coins,1,10,20

```

Then add the vegetation:
1. open `expand_vegetation.yaml` and copy paste the existing `stubbe` entry.
```yaml
# Existing entry with lowered max amount.
- prefab: stubbe
  min: 1
  # From 3 to 2.
  max: 2
  scaleMin: 0.8
  scaleMax: 1.2
  chanceToUseGroundTilt: 1
  biome: Meadows
  maxAltitude: 1000
  maxOceanDepth: 2
  maxTilt: 20
  maxTerrainDelta: 2
# New entry (50% chance of appearing per zone).
- prefab: stubbe
  max: 0.5
  scaleMin: 0.8
  scaleMax: 1.2
  chanceToUseGroundTilt: 1
  biome: Meadows
  maxAltitude: 1000
  maxOceanDepth: 2
  maxTilt: 20
  maxTerrainDelta: 2
  # Add force placement to try finding a free spot 50 times, otherwise the chance of appearing is significantly lowered.
  forcePlacement: true
  # Add data field.
  data: chest_stubbe
```
2. `zones_reset zone start force` to reset the current zone (`zones_reset start force` to reset the entire world).
