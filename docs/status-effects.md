# Status effects

Both the biome and the current environment can apply status effects to the player. Effects can be active during the day, night or both.

## Usage

Status effects are set with the `statusEffects` field in [Biomes](biomes.md), [Environments](environments.md) and [Territories](territories.md).

### Status Effect Fields

- name: Name of the effect.
- requiredGlobalKeys ([list](requirement-lists.md#list)): Active if the world keys are set.
- forbiddenGlobalKeys ([list](requirement-lists.md#list)): Not active if the world keys match.
- requiredPlayerKeys ([list](requirement-lists.md#list)): Active if the player keys are set.
- forbiddenPlayerKeys ([list](requirement-lists.md#list)): Not active if the player keys match.
- day: Active during the day.
- night: Active during the night.
- duration: Duration in seconds. 0 is "permanent". If not given, uses the default duration of the status effect.
  - Duration is not used for damaging effects (Burning, Poison and Spirit).
  - Effects start ticking down when leaving the biome or environment. Effects with "permanent" duration are instantly removed.
- damage: Damage that is affected by both armor and resistances.
  - Burning: Damage per second. Duration is always 5 seconds.
  - Spirit: Damage per second. Duration is always 3 seconds.
  - Poison: Damage over the duration (duration scales with the damage).
- damageIgnoreArmor: Damage that ignores armor.
- damageIgnoreAll: Damage that ignores armor and resistances.
- immuneWithResist: If true, damage resistance counts as immunity.
  - Note: `damageIgnoreAll` is not affected.

See [examples](../examples/examples.md).

## Examples

Night-only freezing effect until Moder is defeated:

```yaml
- environment: Snow
  statusEffects:
  - name: Freezing
    forbiddenGlobalKeys: defeated_dragon
    day: false
    night: true
```

Poison hazard that is disabled for players with a specific progression key:

```yaml
- biome: Swamp
  statusEffects:
  - name: Poison
    forbiddenPlayerKeys: poisonproof, poisonproof_new
    day: true
    night: true
    damage: 18
```
