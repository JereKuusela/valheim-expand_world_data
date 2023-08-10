# Marketplace: Custom trader

Traders from Marketplace can be added to the world generation.

## First you need the data entry

1. `spawn MarketPlaceNPC`
2. Use the UI for edits. For example change model to Wolf and type to transmog.
3. `ew_copy trader_transmog`
4. Open `expand_data.yaml` and remove extra fields

```yaml
- name: trader_transmog
  ints:
  - KGmarketNPC, 9
  strings:
  - KGnpcNameOverride, Transmog
  - KGnpcModelOverride, Wolf
  - KGbyeText, Bye!
```

## Then add the location

1. Open `expand_locations.yaml` and copy paste the existing `Vendor_BlackForest` entry.
2. Add `objectSwap` and `objectData` to the entry.
3. `locations_add Vendor_BlackForest start force` to spawn the location.
4. `find EVendor_BlackForest` to find the location.

```yaml
# New location variant, modify as needed.
- prefab: Vendor_BlackForest:transmog
  biome: BlackForest
  biomeArea: median
  quantity: 10
  minDistance: 0.15
  minAltitude: 1
  prioritized: true
  unique: true
  minDistanceFromSimilar: 512
  iconPlaced: Vendor_BlackForest
  randomRotation: true
  maxTerrainDelta: 2
  exteriorRadius: 12
  clearArea: true
  noBuild: true
  objectSwap:
# Swap the trader.
  - Haldor, MarketPlaceNPC
  objectData:
# Load custom data.
  - MarketPlaceNPC, trader_transmog
```
