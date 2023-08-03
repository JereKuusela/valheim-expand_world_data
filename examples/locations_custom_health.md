# Locations: Custom health

Three ways to set custom health.

## Add entry to `expand_data.yaml`

```yaml
- name: health_80
  floats:
  - health, 80
```

Then use it in the `expand_locations.yaml`.

```yaml
- prefab: BlueprintCastle
  objectData:
  - wood_door, health_80
```

## Use raw data

1. `object health`
2. `object copy=health`
3. Paste to the  `expand_locations.yaml`.

```yaml
- prefab: BlueprintCastle
  objectData:
  - wood_door, AQAAAAJsBBqZA/WeP8wmXssAAKBC
```

## Set health in the blueprint

`hammer_save` automatically includes the health data in the blueprint file.

Note: Data from blueprints has the highest priority. If you want to customize the health, make sure the blueprint data doesn't already set it.
