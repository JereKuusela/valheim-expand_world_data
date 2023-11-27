# Locations: Blueprints

1. Use PlanBuild mod or `hammer_save` to create a blueprint.
2. Add a new entry and use the blueprint filename as the `prefab`.
3. Use `hammer_location` to test the location. Note: The blueprint only shows up after being placed. It's normal to see nothing while placing it.
4. By default, the terrain is smoothly leveled. Adjust the `levelArea`, `levelRadius` and `levelBorder` fields if needed.

Blueprint without leveling:

```yaml
- prefab: Blueprint
  levelArea: false
```

Blueprint with 20 meters flat area:

```yaml
- prefab: Blueprint
  levelRadius: 20
  levelBorder: 0
```

Blueprint with terrain painting:

```yaml
- prefab: Blueprint
  paint: paved
```

Blueprint with 20 meters of paint without border:

```yaml
- prefab: Blueprint
  # dirt,cultivated,paved,vegetation -> mix of cultivated and paved.
  paint: 0,0.5,0.5,0
  paintRadius: 20
  paintBorder: 0
```
