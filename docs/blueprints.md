# Blueprints

Dungeons, locations and vegetation support using blueprints to spawn multiple objects.

Both PlanBuild .blueprint and BuildShare .vbuild files are supported (recommended to use PlanBuild files).

## Configuration

Blueprint files are loaded from the folder set by the `Blueprint folder` setting (default: `PlanBuild`, relative to the config folder). Both the profile and the base Valheim config folders are searched, including subfolders.

Blueprints are loaded when they are first used. Changed files are reloaded automatically unless `Automatic data reload` is disabled.

## File format

The file format is shared with Expand World Prefabs and Infinity Hammer. The same blueprint file can be used with all of them.

- `.blueprint` rows are separated by `;`: `name;unused;posX;posY;posZ;rotX;rotY;rotZ;rotW;info;scaleX;scaleY;scaleZ;`**`data;chance`**
- `.vbuild` rows are separated by space: `name rotX rotY rotZ rotW posX posY posZ`**`data chance`**

The bold fields are extensions made for the Expand World mods and Infinity Hammer. They are optional and always the last columns, so files without them still work. The `.vbuild` format has no scale or info.

### Fields

- name: Object id or name of another blueprint (see Nested blueprints).
- pos, rot: Position and rotation relative to the center of the blueprint. Rotation is a quaternion.
- scale (default: `1,1,1`): Scale of the object.
- data: Initial object data.
  - Either a name of a data entry or the raw base64 data.
  - Infinity Hammer saves this when creating .blueprint files.
- info: Text field of PlanBuild that is used if `data` is empty. The meaning depends on the object:
  - Sign: text.
  - Teleporter: tag.
  - Creature: tamed name.
  - Item stand: `item:variant:quality:orientation`, only the item is required.
  - Armor stand: `pose:slotCount:item:variant:item:variant...`.
  - Item drops (except fish) are marked as placed by a player.
  - Info can contain commas.
- chance (default: `1`): Chance (from 0 to 1) that the object appears. Each object is rolled separately.
  - Can have a group after an underscore, for example `0.5_a`. See Alternatives.

### Alternatives

Objects with the same group (like `0.5_a` and `0.5_a`) are alternatives. One of them is picked and the chances work as probabilities of being the picked one.

- `0.5_a` and `0.5_a`: One of the objects always appears.
- `0.25_a` and `0.25_a`: Half of the time none of them appears.
- `0.9_a` and `0.9_a`: If the total is over 1, the chances are scaled down so that they add up to 1.
- `_a` without a number has chance 1.
- Groups are separate for each blueprint, so nested blueprints can use the same group names.

### Nested blueprints

Blueprints can contain other blueprints as objects.

- These must be added manually to the file.
- This can be useful for larger blueprints that have distinct parts.
- Another use is to make `chance` field to affect multiple objects.
- Real objects are preferred if a blueprint has the same name as an object.
- Maximum depth is 8.
- Terrain of the nested blueprints is also applied. See Terrain snapshots.

### Sections and headers

Lines starting with `#` are headers. Rows after a header belong to that section. Rows before any header are objects.

- `#Pieces`: Objects. This is the default section.
- `#Center:object`: Center piece (bottom center of the blueprint). Default is `piece_bpcenterpoint`.
  - This object is never spawned to the world. If you want to spawn it, duplicate the line manually.
  - Infinity Hammer saves this information automatically to .blueprint files.
  - If the center piece is not found, the blueprint is centered automatically and placed 0.05 meters towards the ground.
- `#SnapPoints`: Position of snap points as rows `x;y;z`. Used by dungeon rooms in Expand World Data.
- `#TerrainHeight`, `#TerrainPaint`: Terrain snapshots. See Terrain snapshots.
- `#Name`, `#Creator`, `#Description`, `#Category`, `#Coordinates`, `#Rotation`: Information that Infinity Hammer uses. These are not used by the Expand World mods.
- `#` alone or followed by a space is a comment and is ignored.
- Unknown headers skip the rows until the next header.

If an object row is invalid, the blueprint is not loaded and the error is logged. Invalid terrain only skips the terrain.

## Terrain snapshots

Blueprint locations and vegetation support the `#TerrainHeight` and `#TerrainPaint` sections written by Infinity Hammer. Terrain is stored as a final height and paint grid rather than as ordered terrain modifier objects. Empty cells leave the destination terrain unchanged.

The section header contains the grid center in XZY order, its capture yaw and the distance between nodes:

```txt
#TerrainHeight:centerX,centerZ,centerY;yaw;nodeSpacing
#TerrainPaint:centerX,centerZ,centerY;yaw;nodeSpacing
```

Height values are absolute heights relative to the blueprint root. Paint values use `r:g:b:a`. Expand World Data applies both channels on the server when the blueprint is generated as a location or vegetation, including every terrain zone covered by the grid. Infinity Hammer is not required on the server or clients.

Blueprint scale does not scale terrain grids. Reapplying the same snapshot is idempotent, but regenerating a location intentionally restores the captured final terrain over later terrain edits within non-empty cells. The stored height values are not limited by this mod. The game limits how much terrain can be changed (vanilla limit is 8 meters from the generated base terrain, some mods change this). Legacy `TerrainModifier` objects are separate from this format and are not replayed as part of the snapshot.

Terrain of nested blueprints is included:

- Chance, alternatives and swaps are resolved first, so only the terrain of objects that actually spawn is used.
- The terrains are merged into one grid for the blueprint. Terrain of later objects overrides earlier ones.
- The merged grid uses the smallest node spacing. Nested blueprints rotated other than by multiples of 90 degrees can be slightly uneven.
- The merged area can't be larger than 2048 x 2048 nodes (or 64 zones). Otherwise the nested terrain is skipped with a warning.
- Nested blueprints are expected to be inside the area of the main blueprint (zones around the blueprint are loaded based on its radius).

Vegetation terrain is applied once for the whole zone after all vegetation is placed. Locations are placed before vegetation and apply their terrain immediately, so vegetation sees the terrain of locations but not of other vegetation. Dungeon rooms don't apply terrain.

When a snapshot reaches neighboring zones, Expand World Data also loads the saved terrain before locations and vegetation when those zones are generated later. If a neighboring zone was already generated before the snapshot location was placed, its previously generated vegetation is not retroactively repositioned even though the terrain snapshot itself is applied.

If any covered terrain compiler is currently owned by a remote peer, the complete snapshot is skipped instead of partially overwriting terrain. The enclosing location still follows vanilla placement and is not retried automatically, so regenerate it after the remote owner has unloaded the area. Terrain-enabled files are an Infinity Hammer/Expand World Data extension and current PlanBuild versions do not load these snapshot rows directly.

## Examples

Nested blueprint object with chance and ZDO data (PlanBuild format):

```txt
MarketplaceStall;0;6;0;4;0;0;0;1;;1;1;1;infinite_health;0.35
```

This line means:

- `MarketplaceStall` is another blueprint object placed inside this blueprint.
- `zdoData` is `infinite_health` (from `data.yaml` or copied raw data).
- `chance` is `0.35`, so it appears 35% of the time.

Two alternative decorations, one of them always appears:

```txt
Pickable_Mushroom;;2;0;1;0;0;0;1;;1;1;1;;0.5_a
Pickable_Thistle;;2;0;1;0;0;0;1;;1;1;1;;0.5_a
```

Sign with text in the info column:

```txt
sign;;0;0;2;0;0;0;1;Welcome, traveler;1;1;1;
```

Center piece marker:

```txt
#center:GlowingMushroom
GlowingMushroom;0;0;0;0;0;0;0;1;;1;1;1;;
```
