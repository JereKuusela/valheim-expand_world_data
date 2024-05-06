# Structure Tweaks: Random damage with infinite health

Infinite health prevents players from destroying your location. However this means that the `randomDamage` field can't be used to make the location look damaged.

Structure Tweaks allows changing the visual style when installed on all clients.

Edit `expand_locations.yaml`:

```yaml
- prefab: BlueprintCastle
  objectData:
  # Each style has 1/3 = 33% chance of appearing.
  - all, st_healthy, st_damaged, st_broken
  # Style can be overridden for individual pieces.
  - wood_door, st_healthy
```

Use weights to modify the chance of each style:

```yaml
- prefab: BlueprintCastle
  objectData:
   # 50% chance for damaged.
  - all, st_healthy, st_damaged:2, st_broken
```

If these don't work, check that `data.yaml` has the following:

```yaml
- name: st_healthy
  ints:
  - override_wear, 0
  floats:
  - health, 1E30
- name: st_damaged
  ints:
  - override_wear, 1
  floats:
  - health, 1E30
- name: st_broken
  ints:
  - override_wear, 3
  floats:
  - health, 1E30
```
