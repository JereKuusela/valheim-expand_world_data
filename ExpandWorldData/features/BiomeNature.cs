using HarmonyLib;
namespace ExpandWorldData;

[HarmonyPatch(typeof(Beehive), nameof(Beehive.CheckBiome))]
public class BeehiveCheckBiome
{
  static void Prefix() => HeightmapFindBiome.Nature = true;
  static void Finalizer() => HeightmapFindBiome.Nature = false;
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
public class PlayerUpdatePlacementGhost
{
  static void Prefix() => HeightmapFindBiome.Nature = true;
  static void Finalizer() => HeightmapFindBiome.Nature = false;
}
[HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
public class PlantUpdateHealth
{
  static void Prefix() => GetBiomeHM.Nature = true;
  static void Finalizer() => GetBiomeHM.Nature = false;
}
[HarmonyPatch(typeof(Heightmap), nameof(Heightmap.GetGroundMaterial))]
public class HeightmapGetGroundMaterial
{
  static void Prefix() => GetBiomeHM.Nature = true;
  static void Finalizer() => GetBiomeHM.Nature = false;
}

