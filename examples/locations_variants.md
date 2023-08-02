# Locations: Variants

Instead of editing existing locations, you can create new ones.

## Replace objects

1. Open `expand_locations.yaml` and copy paste the existing `Dolmen01` entry
```yaml
- prefab: Dolmen01:Ghost
  biome: Meadows, BlackForest
  quantity: 100
  minAltitude: 1
  randomRotation: true
  maxTerrainDelta: 2
  exteriorRadius: 8
  randomDamage: true
# Replaces the skeleton with a ghost.
  objectSwap:
    - Spawner_Skeleton_night_noarcher, Spawner_Ghost
```
2. `locations_add Dolmen01:Ghost start force` to spawn the location.
3. `find Dolmen01:Ghost` to find the location.

## Replace objects in dungeons
```yaml
- prefab: Crypt2:Greydwarf
  objectSwap:
    - Spawner_Skeleton, Spawner_Greydwarf
    - Spawner_Ghost, Spawner_Greydwarf_Shaman
    - Spawner_Skeleton_poison, Spawner_Greydwarf_Elite
    - BonePileSpawner, Spawner_Greydwarf_Elite
```

## Add new objects
1. Add something easily visible to the center:
```yaml
- prefab: StoneTowerRuins05:Draugr
  objects:
    - GlowingMushroom
```
2. Move the object and reset the zone until you find a good spot (`zones_reset zone start`):
```yaml
- prefab: StoneTowerRuins05:Draugr
  objects:
    - GlowingMushroom, 4.5, -4.5
```
3. Finalize:
```yaml
- prefab: StoneTowerRuins05:Draugr
  objects:
    - Spawner_DraugrPile, 4.5, -4.5
    - Spawner_DraugrPile, -4.5, -4.5
    - Spawner_DraugrPile, 4.5, 4.5
    - Spawner_DraugrPile, -4.5, 4.5
  objectSwap:
# Replaces skeletons with draugr (2:1 ratio for normal and elites).
# 2:1 ratio = 66% chance for normal and 33% chance for elite.
    - Spawner_Skeleton, Spawner_Draugr:2, Spawner_Draugr_Elite
# Replaces the default spawner.
    - BonePileSpawner, Spawner_DraugrPile
```
4. If you have trouble finding the right spots:
  - Add GlowingMushroom to the center and reset.
  - Build the location by hand with Infinity Hammer (`hammer Spawner_DraugrPile`).
  - Select the location with the area select tool.
  - `hammer_save Test GlowingMushroom`
  - Open the blueprint and file check coordinates for the Spawner_DraugrPile.
  - Note: Blueprint has coordinates in x;y;z format while Expand World uses x,z,y format.