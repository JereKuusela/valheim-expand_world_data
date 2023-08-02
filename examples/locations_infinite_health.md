# Locations: Infinite health

Infinite health can be used to prevent players from destroying your location.

Open `expand_locations.yaml`:
```yaml
- prefab: BlueprintCastle
  objectData:
  # Infinite health for all objects.
  - all, infinite_health
  # Health can be overridden for individual objects.
  - wood_door, default_health
```

If these don't work, check that `expand_data.yaml` has the following:
```yaml
- name: default_health
  floats:
  - health, 0
- name: infinite_health
  floats:
  - health, 1E30
```