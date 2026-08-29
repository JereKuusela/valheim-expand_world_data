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

## Terrain snapshots

Blueprint locations support the `#TerrainHeight` and `#TerrainPaint` sections written by Infinity Hammer. Terrain is stored as a final height and paint grid rather than as ordered terrain modifier objects. Empty cells leave the destination terrain unchanged.

The section header contains the grid center in XZY order, its capture yaw and the distance between nodes:

```txt
#TerrainHeight:centerX,centerZ,centerY;yaw;nodeSpacing
#TerrainPaint:centerX,centerZ,centerY;yaw;nodeSpacing
```

Height values are absolute heights relative to the blueprint root. Paint values use `r:g:b:a`. Expand World Data applies both channels on the server when the blueprint is generated as a location, including every terrain zone covered by the grid. Infinity Hammer is not required on the server or clients.

Blueprint scale does not scale terrain grids. Reapplying the same snapshot is idempotent, but regenerating a location intentionally restores the captured final terrain over later terrain edits within non-empty cells. Vanilla terrain compilers clamp height changes to 8 meters from the generated base terrain. Legacy `TerrainModifier` objects are separate from this format and are not replayed as part of the snapshot.

When a snapshot reaches neighboring zones, Expand World Data also loads the saved terrain before locations and vegetation when those zones are generated later. If a neighboring zone was already generated before the snapshot location was placed, its previously generated vegetation is not retroactively repositioned even though the terrain snapshot itself is applied.

Terrain sections are applied only for the top-level blueprint location, not for blueprint objects nested inside another blueprint. If any covered terrain compiler is currently owned by a remote peer, the complete snapshot is skipped instead of partially overwriting terrain. The enclosing location still follows vanilla placement and is not retried automatically, so regenerate it after the remote owner has unloaded the area. Terrain-enabled files are an Infinity Hammer/Expand World Data extension and current PlanBuild versions do not load these snapshot rows directly.

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
