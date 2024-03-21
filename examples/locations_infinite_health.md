# Locations: Unbreakable locations

Infinite health can be used to prevent players from destroying your location.

Open `data.yaml`:

```yaml
- name: invulnerable_piece
  floats:
  - health, -1
  - WearNTear.m_health, -1
- name: invulnerable_destructible
  ints:
  - Destructible.m_minToolTier, 9999
- name: default_health
  floats:
  - health, <none>
```

Open `expand_locations.yaml`:

```yaml
- prefab: BlueprintCastle
  objectData:
  # Infinite health for all pieces.
  - WearNTear, invulnerable_piece
  # Infinite health for other destructibles.
  - Destructible, invulnerable_destructible
  # Health can be overridden for individual objects.
  - wood_door, default_health
```

## Unbreakable randomly damaged pieces

Open `data.yaml`:

```yaml
- name: infinite_worn
  floats:
  - health, -1
  - WearNTear.m_health, -2
- name: infinite_damaged
  floats:
  - health, -1
  - WearNTear.m_health, -4
```

Open `expand_locations.yaml`:

```yaml
- prefab: BlueprintCastle
  objectData:
  # 25% healthy, 50% worn, 25% damaged.
  - WearNTear, infinite_health, infinite_worn:2, infinite_damaged
```
