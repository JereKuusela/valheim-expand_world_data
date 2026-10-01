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
- snowBuildup (default: `0`): How quickly snow builds up on structures.
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

Note: As you can see, lots of values have unknown meaning. Probably better to look at the existing environments for inspiration.
