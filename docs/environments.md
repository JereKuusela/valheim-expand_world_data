# Environments

The file `expand_environments.yaml` sets the available weathers.

Command `ew_musics` can be used to print available musics.

- name: Identifier to be used in other files.
- particles: Identifier of a default environment to set particles. Required for new environments.
- isDefault (default: `false`): The first default environment is loaded at the game start up. No need to set this true unless removing from the Clear environment.
- isWet (default: `false`): If true, is considered to be raining.
- isFreezing (default: `false`): If true, causes the freezing debuff.
- isFreezingAtNight (default: `false`): If true, causes the freezing at night.
- isCold (default: `false`): If true, causes the cold debuff.
- isColdAtNight (default: `false`): If true, causes the cold at night.
- alwaysDark (default: `false`): If true, causes constant darkness.
- windMin (default: `0.0`): The minimum wind strength.
- windMax (default: `1.0`): The maximum wind strength.
- rainCloudAlpha (default: `0.0`): Amount of clouds in the sky.
- ambientVol (default: `0.3`): ???.
- ambientList: ???.
- musicMorning: Music override for the morning time. Higher priority than the biome value.
- musicDay: Music override for the day time. Higher priority than the biome value.
- musicEvening: Music override for the evening time. Higher priority than the biome value.
- musicNight: Music override for the night time. Higher priority than the biome value.
- ambColorDay, ambColorNight, sunColorMorning, sunColorDay, sunColorEvening, sunColorNight: Color values.
- fogColorMorning, fogColorDay, fogColorEvening, fogColorNight, fogColorSunMorning, fogColorSunDay, fogColorSunEvening, fogColorSunNight: Color values.
- fogDensityMorning, fogDensityDay, fogDensityEvening, fogDensityNight (default: `0.01`): ???.
- lightIntensityDay (default: `1.2`): ???.
- lightIntensityNight (default: `0`): ???.
- sunAngle (default: `60`): ???.
- statusEffects: List of status effects that are active in this environment.
  - See [Status effects](status-effects.md) for format and more information.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.

Note: As you can see, lots of values have unknown meaning. Probably better to look at the existing environments for inspiration.

## Examples

Wind profile only:

```yaml
- name: TempestNight
  particles: ThunderStorm
  windMin: 0.65
  windMax: 1.0
```

Music override only:

```yaml
- name: TempestNight
  particles: ThunderStorm
  musicDay: music_mountains
  musicNight: music_swamp
```

Lighting and sky color tuning only:

```yaml
- name: CrystalDawn
  particles: Clear
  ambColorDay: 1, 0.93, 0.86, 1
  sunColorMorning: 1, 0.74, 0.55, 1
  fogColorMorning: 0.86, 0.8, 0.72, 1
  fogColorSunMorning: 1, 0.78, 0.62, 1
  lightIntensityDay: 1.35
  lightIntensityNight: 0.08
```
