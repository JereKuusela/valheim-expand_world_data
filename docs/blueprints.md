# Blueprints

Dungeons, locations and vegetation support using blueprints to spawn multiple objects.

Both PlanBuild .blueprint and BuildShare .vbuild files are supported (recommended to use PlanBuild files).

The file format has new features:

- zdoData that is used to initialize the object data.
  - Infinity Hammer automatically saves this when creating .blueprint files.
- chance field (from 0 to 1) that determines the chance of the object appearing. This must be added manually to the file.
- Formats with the new fields:
  - .blueprint format: name;unused;posX;posY;posZ;rotX;rotT;rotZ;rotW;info;scaleX;scaleY;scaleZ;**zdoData;chance**
  - .vbuild format: name;rotX;rotT;rotZ;rotW;posX;posY;posZ;**zdoData;chance**
- Blueprints can contain other blueprints as objects.
  - These must added manually to the file.
  - This can be useful for larger blueprints that have distinct parts.
  - Another use is to make `chance` field to affect multiple objects.
- Center piece (bottom center of the blueprint) can be set to a certain object.
  - This object is never spawned to the world. If you want to spawn it, duplicate the line manually.
  - Infinity Hammer saves this information automatically to .blueprint files.
  - If the center piece is not found, the blueprint is centered automatically and placed 0.05 meters towards the ground.

## Examples

Nested blueprint object with chance and ZDO data (PlanBuild format):

```txt
MarketplaceStall;0;6;0;4;0;0;0;1;;1;1;1;infinite_health;0.35
```

This line means:

- `MarketplaceStall` is another blueprint object placed inside this blueprint.
- `zdoData` is `infinite_health` (from `data.yaml` or copied raw data).
- `chance` is `0.35`, so it appears 35% of the time.

Center piece marker:

```txt
#center:GlowingMushroom
GlowingMushroom;0;0;0;0;0;0;0;1;;1;1;1;;
```
