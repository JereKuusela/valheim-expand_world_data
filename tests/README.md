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

## Location icon registration regression

`fixtures/expand_locations_icon_regression.yaml` exercises the server's icon-token selection and the client's sprite/size handling with vanilla assets only. It is intended for a disposable test world.

1. Install EWD on a dedicated server and a client. Enable `Location data` on the server, copy the fixture into its `BepInEx/config/expand_world` directory, and generate a new test world. Confirm that the two cloned location types have generated instances; missing instances are a generation issue, not this icon regression.
2. Connect with a client that has no local copy of the fixture and leave its local `Location data` disabled. Open the map before visiting the generated locations.
3. The `StartTemple:icon_alias` instances should use the vanilla `StartTemple` sprite. The `StartTemple:icon_item` instances should use the deer trophy sprite with world size 100. The transmitted tokens should be `StartTemple` and `TrophyDeer, 100`, not the clone prefab names. Receiving a nonempty location-icon RPC alone does not confirm that the sprites can be resolved.
4. Repeat as a local host. Ordinary vanilla location icons should remain unchanged in both roles.

Additional checks:

- Replace the item entry's `iconAlways` with `TrophyDeer, 2, true` in a fresh test session: the relative size multiplier and animation should be applied. Do not use a vanilla location-sprite name with a size suffix; the existing fallback resolver supports pin types, items and status effects.
- Set `iconPlaced: TrophyDeer, 100` on the alias entry. Before the location is placed it should show `StartTemple`; after placement it should use the trophy token. The existing slight Y offset permits the pin to be recreated for this transition.
- Toggle the server's `Location data` off and on and inspect the **first** icon broadcast after each reload. The server list prefix should be removed/applied before that broadcast, without waiting for the deferred patch refresh. Clients must continue reading their received icon cache, even when their local `Location data` is disabled.
- Reconnect or change worlds and confirm that relative sizes do not carry over from old pins. Repeated patch refreshes should not register duplicate hooks.

These are manual checks, not an automated game test. An already-created client pin may keep its sprite when only the token changes at the same position; reconnect before judging that visual reload case. This existing behavior is separate from missing Harmony registrations.
