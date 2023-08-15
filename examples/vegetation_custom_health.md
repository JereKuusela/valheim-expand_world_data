# Vegetation: Custom health

Three ways to set custom health.

## Add entry to `expand_data.yaml`

```yaml
- name: health_200
  floats:
  - health, 200
```

Then use it in the `expand_vegetation.yaml`.

```yaml
- prefab: Oak
  data: health_200
```

## Use raw data

1. `object health=80`
2. `object copy=health`
3. Paste to the  `expand_locations.yaml`.

```yaml
- prefab: Oak
  data: AQAAAAJsBBqZA/WeP8wmXssAAKBC
```

## Use blueprint

Blueprints can be used as vegetation. `hammer_save` automatically includes the health data in the blueprint file.

Note: Data from blueprints has the highest priority. If you want to customize the health, make sure the blueprint data doesn't already set it.
