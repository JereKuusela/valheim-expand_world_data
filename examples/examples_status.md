# Status effect examples

```
  - statusEffects:
    # Grants extra carry capacity.
    # Removed when leaving the biome / environment.
    - name: BeltStrength
      duration: 0
    # Grants 10 seconds of corpse run.
    # Expires after 10 seconds when leaving the biome / environment.
    - name: CorpseRun
      duration: 10
    # Causes 50 burning damage per second, during the day time.
    # Burning always lasts 5 seconds for 250 damage.
    # Damage is not reduced by armor.
    # Fire resistance causes full immunity.
    - name: Burning
      damageIgnoreArmor: 50
      day: true
      immuneWithResist: true
    # With Poison resist and 25 armor would deal:
    # 25 + 50 / 2 + (100 / 2 - 25) = 25 + 25 + 25 = 75 damage over time.
    - name: Poison
      damage: 100
      damageIgnoreArmor: 50
      damageIgnoreAll: 25
```
