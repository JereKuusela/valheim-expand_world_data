# Biomes: Add new

1. Open `expand_biomes.yaml`.
2. Copy-paste existing entry.
3. Change `biome`, add `name` and add `terrain`.
4. Add/modify other fields.
5. Open `expand_world.yaml`.
6. Look at it until the rules start to make sense.
7. Determine how you want the new biome to appear on the world.
8. Make changes until you succeed.
9. Edit other files to make spawns, locations and vegetation to work.

Example:

Copy-paste ashlands entry and change:

    - biome: desert
      name: Desert
      terrain: ashlands
      environments:
      - environment: Clear

Copy-paste plains entry and change the top one:

    - biome: desert
      amount: 0.5
