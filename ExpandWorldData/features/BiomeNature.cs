namespace ExpandWorldData;

public class BeehiveCheckBiome
{
  internal static void BeginNatureBiomeCheck() => HeightmapFindBiome.Nature = true;
  internal static void EndNatureBiomeCheck() => HeightmapFindBiome.Nature = false;
}

public class PlayerUpdatePlacementGhost
{
  internal static void BeginNatureBiomeCheck() => HeightmapFindBiome.Nature = true;
  internal static void EndNatureBiomeCheck() => HeightmapFindBiome.Nature = false;
}

public class PlantUpdateHealth
{
  internal static void BeginNatureBiomeCheck() => GetBiomeHM.Nature = true;
  internal static void EndNatureBiomeCheck() => GetBiomeHM.Nature = false;
}

public class HeightmapGetGroundMaterial
{
  internal static void BeginNatureBiomeCheck() => GetBiomeHM.Nature = true;
  internal static void EndNatureBiomeCheck() => GetBiomeHM.Nature = false;
}

