# Status effects

Both the biome and the current environment can apply status effects to the player. Effects can be active during the day, night or both.

Each entry has following fields:

- `name`: Name of the effect.
- `requiredGlobalKeys`: Active if all of these world keys are set.
- `forbiddenGlobalKeys`: Active if none of these world keys are set.
- `requiredPlayerKeys`: Active if all of these player keys are set.
- `forbiddenPlayerKeys`: Active if none of these player keys are set.
- `day`: Active during the day.
- `night`: Active during the night.
- `duration`: Duration in seconds. 0 is "permanent". If not given, uses the default duration of the status effect.
  - Duration is not used for damaging effects (Burning, Poison and Spirit).
  - Effects start ticking down when leaving the biome or environment. Effects with "permanent" duration are instantly removed.
- `damage`: Damage that is affected by both armor and resistances.
  - Burning: Damage per second. Duration is always 5 seconds.
  - Spirit: Damage per second. Duration is always 3 seconds.
  - Poison: Damage over the duration (duration scales with the damage).
- `damageIgnoreArmor`: Damage that ignores armor.
- `damageIgnoreAll`: Damage that ignores armor and resistances.
- `immuneWithResist`: If true, damage resistance counts as immunity.
  - Note: `damageIgnoreAll` is not affected.

See [examples](https://github.com/JereKuusela/valheim-expand_world_data/blob/main/examples/examples.md).
