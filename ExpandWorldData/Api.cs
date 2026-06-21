
namespace ExpandWorldData;

public static class Api
{
  public static float GetMinimapHeight(float height, Heightmap.Biome biome)
  {
    if (!BiomeManager.TryGetData(biome, out var data)) return height;
    if (height < WorldInfo.WaterLevel) return height;
    return (height - WorldInfo.WaterLevel) * data.mapColorMultiplier + WorldInfo.WaterLevel;
  }
  public static void AddBiome(BiomeYaml data)
  {
    BiomeManager.AddBiome(data);
  }
  public static void AddTerritory(TerritoryYaml data)
  {
    TerritoryManager.AddTerritory(data);
  }
  public static void AddClutter(ClutterYaml data)
  {
    ClutterManager.AddClutter(data);
  }
  public static void AddDungeon(DungeonYaml data)
  {
    Dungeon.Loader.AddDungeon(data);
  }
  public static void AddLocation(LocationYaml data)
  {
    LocationLoading.AddLocation(data);
  }
  public static void AddRoom(RoomYaml data)
  {
    RoomLoading.AddRoom(data);
  }
  public static void AddVegetation(VegetationYaml data)
  {
    VegetationLoading.AddVegetation(data);
  }
  public static void ChangeWorld(WorldYaml data, int index)
  {
    WorldManager.AddWorld(data, index);
  }
}