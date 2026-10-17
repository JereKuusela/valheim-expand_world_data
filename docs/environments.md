# Environments

Environments are the available weathers.

## Configuration

The file `expand_environments.yaml` is created automatically when loading a world. Enable or disable environments from the config using the `Environment data` setting.

Command `ew_musics` can be used to print available musics.

### Environment Fields

- name: Identifier to be used in other files.
- particles: Identifier of a default environment to set particles. Required for new environments.
- isDefault (default: `false`): The first default environment is loaded at the game start up. No need to set this true unless removing from the Clear environment.
- isWet (default: `false`): If true, is considered to be raining.
- isFreezing (default: `false`): If true, causes the freezing debuff.
- isFreezingAtNight (default: `false`): If true, causes the freezing at night.
- isCold (default: `false`): If true, causes the cold debuff.
- isColdAtNight (default: `false`): If true, causes the cold at night.
- alwaysDark (default: `false`): If true, causes constant darkness.
- snowBuildup (default: `0`): How quickly snow builds up on structures.
- windMin (default: `0.0`): The minimum wind strength.
- windMax (default: `1.0`): The maximum wind strength.
- rainCloudAlpha (default: `0.0`): Amount of clouds in the sky.
- ambientVol (default: `0.3`): Volume of the ambient loop sound.
- ambientList: Name of the ambient sound list used for random ambient sounds. Must match a list in the game's audio manager.
- musicMorning: Music override for the morning time. Higher priority than the biome value.
- musicDay: Music override for the day time. Higher priority than the biome value.
- musicEvening: Music override for the evening time. Higher priority than the biome value.
- musicNight: Music override for the night time. Higher priority than the biome value.
- ambColorDay, ambColorNight, sunColorMorning, sunColorDay, sunColorEvening, sunColorNight: Color values.
- fogColorMorning, fogColorDay, fogColorEvening, fogColorNight, fogColorSunMorning, fogColorSunDay, fogColorSunEvening, fogColorSunNight: Color values.
- fogDensityMorning, fogDensityDay, fogDensityEvening, fogDensityNight (default: `0.01`): Fog density at each time of day. Higher value means thicker fog.
- lightIntensityDay (default: `1.2`): Intensity of the directional light during the day.
- lightIntensityNight (default: `0`): Intensity of the directional light during the night.
- sunAngle (default: `60`): Elevation angle of the directional light's orbit in degrees.
- auroraIntensityMorning, auroraIntensityDay, auroraIntensityEvening, auroraIntensityNight (default: `0`): Strength of the aurora (northern lights) at each time of day.
- auroraColors (default: no aurora): Aurora gradient as a list of color keys. Format is `time, r, g, b` where time is from 0.0 to 1.0.
  - Maximum of 8 keys. Alpha is not supported.
  - Example: `auroraColors: ["0, 0.1, 0.9, 0.4", "1, 0.8, 0.2, 0.6"]`.
- cloudOpacityMorning, cloudOpacityDay, cloudOpacityEvening, cloudOpacityNight (default: `8.53`): Opacity of the clouds at each time of day.
- aoIntensityNight (default: `1`), aoIntensityMorning (default: `0.85`), aoIntensityDay (default: `0.8`), aoIntensityEvening (default: `0.85`): Ambient occlusion intensity at each time of day.
- colorAmbientOcclusion (default: `0.005, 0, 0.226`): Ambient occlusion color.
- psystemsOutsideOnly (default: `false`): If true, the particles are only shown outside.
- statusEffects: List of status effects that are active in this environment.
  - See [Status effects](status-effects.md) for format and more information.
  - Note: Normal effects are still active. There is no point to add Freezing to non-freezing environments.

## Examples

New foggy environment. The fields `name` and `particles` (an existing environment to copy the particles from) are required for new environments:

```yaml
- name: GreenFog
  particles: Misty
  windMax: 0.2
  fogDensityMorning: 0.04
  fogDensityDay: 0.03
  fogDensityEvening: 0.04
  fogDensityNight: 0.05
  colorFogMorning: 0.25, 0.45, 0.25, 0
  colorFogDay: 0.3, 0.5, 0.3, 0
  colorFogEvening: 0.25, 0.4, 0.25, 0
  colorFogNight: 0.1, 0.2, 0.1, 0
```

The new environment is then used from a biome:

```yaml
- biome: Swamp
  environments:
  - environment: SwampRain
    weight: 3
  - environment: GreenFog
```

Calmer weather for an existing environment:

```yaml
- name: Clear
  windMax: 0.3
```

Colder nights for an existing environment:

```yaml
- name: Rain
  statusEffects:
  - name: Cold
    day: false
    night: true
```
