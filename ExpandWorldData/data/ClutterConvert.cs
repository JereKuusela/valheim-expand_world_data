using Service;

namespace ExpandWorldData;

public partial class ClutterManager
{
  public static ClutterSystem.Clutter FromData(ClutterYaml data, string fileName)
  {
    ClutterSystem.Clutter clutter = new();
    if (Prefabs.TryGetValue(data.prefab, out var prefab))
      clutter.m_prefab = prefab;
    else
      Log.Warning($"{fileName}: Failed to find clutter prefab {data.prefab}.");
    clutter.m_enabled = data.enabled;
    clutter.m_amount = data.amount;
    clutter.m_biome = DataManager.ToBiomes(data.biome, fileName);
    clutter.m_instanced = data.instanced;
    clutter.m_onUncleared = data.onUncleared;
    clutter.m_onCleared = data.onCleared;
    clutter.m_scaleMin = data.scaleMin;
    clutter.m_scaleMax = data.scaleMax;
    clutter.m_maxTilt = data.maxTilt;
    clutter.m_minTilt = data.minTilt;
    clutter.m_maxAlt = data.maxAltitude;
    clutter.m_minAlt = data.minAltitude;
    clutter.m_maxVegetation = data.maxVegetation;
    clutter.m_minVegetation = data.minVegetation;
    clutter.m_snapToWater = data.snapToWater;
    clutter.m_terrainTilt = data.terrainTilt;
    clutter.m_randomOffset = data.randomOffset;
    clutter.m_minOceanDepth = data.minOceanDepth;
    clutter.m_maxOceanDepth = data.maxOceanDepth;
    clutter.m_inForest = data.inForest;
    clutter.m_forestTresholdMin = data.forestTresholdMin;
    clutter.m_forestTresholdMax = data.forestTresholdMax;
    clutter.m_fractalScale = data.fractalScale;
    clutter.m_fractalOffset = data.fractalOffset;
    clutter.m_fractalTresholdMin = data.fractalThresholdMin;
    clutter.m_fractalTresholdMax = data.fractalThresholdMax;
    return clutter;
  }

  public static ClutterYaml ToData(ClutterSystem.Clutter clutter) => new()
  {
    prefab = clutter.m_prefab.name,
    enabled = clutter.m_enabled,
    amount = clutter.m_amount,
    biome = DataManager.FromBiomes(clutter.m_biome),
    instanced = clutter.m_instanced,
    onUncleared = clutter.m_onUncleared,
    onCleared = clutter.m_onCleared,
    scaleMin = clutter.m_scaleMin,
    scaleMax = clutter.m_scaleMax,
    maxTilt = clutter.m_maxTilt,
    minTilt = clutter.m_minTilt,
    maxAltitude = clutter.m_maxAlt,
    minAltitude = clutter.m_minAlt,
    maxVegetation = clutter.m_maxVegetation,
    minVegetation = clutter.m_minVegetation,
    snapToWater = clutter.m_snapToWater,
    terrainTilt = clutter.m_terrainTilt,
    randomOffset = clutter.m_randomOffset,
    minOceanDepth = clutter.m_minOceanDepth,
    maxOceanDepth = clutter.m_maxOceanDepth,
    inForest = clutter.m_inForest,
    forestTresholdMin = clutter.m_forestTresholdMin,
    forestTresholdMax = clutter.m_forestTresholdMax,
    fractalScale = clutter.m_fractalScale,
    fractalOffset = clutter.m_fractalOffset,
    fractalThresholdMin = clutter.m_fractalTresholdMin,
    fractalThresholdMax = clutter.m_fractalTresholdMax
  };
}
