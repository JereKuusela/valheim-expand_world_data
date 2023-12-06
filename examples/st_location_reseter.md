# Spawner Tweaks: Location reset

Spawner Tweaks can be used to create a custom altar that resets the location, for a price.

This requires Upgrade World installed on the server.

## First you need the data entry

1. `spawn GlowingMushroom`
2. `tweak_altar amount=-1 name="Location reset" command="zones_reset zone={i},{j} start force"`
3. `ew_copy reseter`
4. Open `expand_data.yaml` and modify the entry to this:

```yaml
- name: reseter
  ints:
  - override_amount, -1
  strings:
  - override_name, Location reset
  - override_component, altar
  - override_command, zones_reset zone={i},{j} start force

```

## Then add the object

1. Open `expand_locations.yaml` and find `Runestone_Boars` entry.
2. Add to it:

```yaml
  objects:
  - GlowingMushroom,0,-10,snap
  objectData:
  - GlowingMushroom,reseter
```

Then find a boar runestone and use `zones_reset zone start force` to reset it.

Now you can interact with the mushroom to reset the location.

## Futher modifications

You can add cost to the reset.

1. `tweak_altar amount=10 spawnitem=Coins name="Location reset"  command="zones_reset zone=<i>,<j> start force"`
2. `ew_copy coin_reseter`
3. Open `expand_data.yaml` and modify the entry to this:

```yaml
- name: coin_reseter
  ints:
  - override_amount, 10
  strings:
  - override_spawn_item, Coins
  - override_component, altar
  - override_name, Location reset
  - override_command, zones_reset zone=<i>,<j> start force
```

```yaml
  objects:
  - GlowingMushroom,0,-10,snap
  objectData:
  - GlowingMushroom,coin_reseter
```
