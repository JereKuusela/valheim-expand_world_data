
namespace ExpandWorldData;

public static class Api
{
  public static void AddBiome(BiomeYaml data)
  {
    BiomeManager.AddBiome(data);
  }
  public static void ChangeWorld(WorldYaml data, int index)
  {
    WorldManager.AddWorld(data, index);
  }
}