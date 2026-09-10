# Test fixtures

`fixtures/terrain-blueprint-contract.blueprint` is a minimal interoperability and manual regression fixture for terrain snapshots.

It deliberately places the first piece at X = -2 while the terrain grid is centered at X = 1. This catches multi-piece placement code that incorrectly uses the first child instead of the blueprint root. The height and paint sections are both 3 columns by 2 rows and contain one empty cell, which consumers must leave unchanged.

Manual round-trip check:

1. Load and place the fixture at a known position and yaw.
2. Confirm that the terrain grid is positioned from the blueprint root, not from the first floor piece.
3. Save the active selection under a new name.
4. Place it immediately, then reload the saved file and place it again at the same position and yaw on clean terrain.
5. Compare the affected nodes. Both placements must use the same anchor, search radius, height values, and paint values.

## Rotated center regression

`fixtures/terrain-rotated-center.blueprint` uses a center piece at `(2, 1, 0)` with yaw 90 degrees, while both terrain channels start at center `(5, 10, 7)` with reference yaw 30 degrees.

After Expand World Data parses the fixture and runs `Blueprint.Center()` using its `#Center:wood_floor` header:

- The center piece must be at the origin with identity rotation.
- The terrain center must be approximately `(3.268, 9, 8)` in X, Y, Z order.
- The terrain reference yaw must be approximately 120 degrees.
- Height rows must retain their order and become `10;11` and `12;13` after subtracting the root Y offset.
- Paint rows must remain unchanged.

Repeat placement at yaw 0, 90 and 180 degrees. The terrain must retain the same offset and orientation relative to both pieces.

## Future section regression

`fixtures/terrain-future-section.blueprint` puts valid-looking payload rows after unknown and malformed headers. A parser must:

- Load exactly one piece (`wood_floor`), never `future_payload`.
- Load a 2 by 2 height grid; `999` and `888` must not be appended to it.
- Load the following 2 by 2 paint grid normally.

## Alternate-biome vegetation regression

Use a native vegetation list where the same prefab has both an ordinary row and an alternate-biome-owned row.

1. Load legacy YAML for that prefab with `altBiome` omitted. The resolved row must have no alternate-biome owner.
2. Load YAML with an explicit `altBiome` value. The resolved row must retain that exact owner.
3. Enable automatic migration with only the ordinary row represented. The alternate row must be appended separately.
4. Repeat with only the alternate row represented. The ordinary row must be appended separately.
5. Confirm `ShouldHonorAlternateBiomeVegetationBlock` returns false for null or blank owners and true for a named owner.
6. Generate fresh, eligible zones. Ordinary vegetation must remain present throughout the base biome and the alternate-biome interior, while the alternate row remains restricted to its named alternate biome.
7. With multiple compatible alternate biomes active, confirm the parent row is evaluated once and each valid owner row is evaluated once. A row owned by a different inactive alternate biome must remain ineligible.
8. Reload vegetation configuration and generate another fresh zone. Confirm that base rows and owner rows have not compounded or duplicated.
9. Overlap two complete owners that contain the same inherited definition. Confirm the first active owner evaluates it once and the later owner does not evaluate an identical copy.
10. Add a definition unique to the later owner. Confirm that unique definition remains eligible in the overlap.
11. Repeat with three complete owners sharing the definition. Confirm one copy is eligible and two copies are skipped.
12. Give an eligible vegetation row a uniform non-default scale, unload the zone, and reload it. Confirm the transform and synchronized vector-scale value retain the generated scale.

## Single-player location icon regression

1. Run against an unpublicized Valheim runtime assembly where `ZoneSystem.tempIconList` is private.
2. Create a fresh single-player world and allow `Game.FindSpawnPoint` to poll `ZoneSystem.GetLocationIcon`.
3. Confirm EWD's prefix receives Harmony's `___tempIconList` field injection and performs no direct field access.
4. Confirm spawn lookup completes without `FieldAccessException` and the player enters the world.

## First-launch defaults and vegetation reloads (1.70.1 prototype)

The numbered source handoff includes a separate build/test harness for this change. It exercises compiled schema types with the real YamlDotNet serializer and the production file-event batcher with controlled time/snapshots.

Manual checks:

1. Use an isolated test profile with no existing `expand_world` configuration and a new disposable world. Generated defaults must preserve disabled locations and vegetation as `enabled: false`, along with explicit zero values.
2. Confirm initialization finishes and vegetation loads once after generating all files. Delayed filesystem notifications for that same content must not trigger another load.
3. Edit several vegetation files together. Expect one reload after approximately 0.5 seconds without further changes, and verify the resulting vegetation settings apply.
4. Repeat with automatic data reload disabled: initial generated vegetation still loads synchronously.
5. Quit/rejoin and repeat on a dedicated server. Pending reload requests must not retain old world state.

Existing files are never automatically overwritten. Earlier generated YAML may already have lost disabled/zero values; regenerate only disposable default files or review them against native defaults. Hand-authored Enigma configuration should be retained.

The empty-biome exception is not suppressed by this patch. Preserving disabled defaults removes a confirmed path that can activate unwanted locations, but disappearance of the reported exception needs the clean first-launch playtest. Native dungeon-door lookup warnings and ordinary placement shortfalls are separate findings.
