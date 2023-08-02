# Locations: Custom health

Three ways to set custom health.

1. Open `expand_data.yaml` and add new entry:
```
- name: health_80
  floats:
  - health, 80

```
Then use it in the `expand_locations.yaml`:
```
- prefab: BlueprintCastle
  objectData:
  - wood_door, health_80
```

2. Use `object health` on any object and then copy the data with `object copy`.

Then use it in the `expand_locations.yaml`:
```
- prefab: BlueprintCastle
  objectData:
  - wood_door, AQAAAAJsBBqZA/WeP8wmXssAAKBC
```

3. For blueprints, `hammer_save` automatically includes the health data in the blueprint file.

Note: Data from blueprints has the highest priority. If you want to customize the health, make sure the blueprint data doesn't already set it.