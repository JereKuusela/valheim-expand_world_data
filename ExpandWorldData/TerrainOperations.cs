using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ExpandWorldData;
using Operation = Action<int, float>;
public class TerrainNode
{
  public int Index;
  public float Distance;
}

public partial class Terrain
{
  public static Dictionary<string, Color> Paints = new() {
    {"grass", Color.black},
    {"patches", new(0f, 0.75f, 0f)},
    {"grass_dark", new(0.6f, 0.5f, 0f)},
    {"dirt", Color.red},
    {"cultivated", Color.green},
    {"paved", Color.blue},
    {"paved_moss", new(0f, 0f, 0.5f)},
    {"paved_dirt", new(1f, 0f, 0.5f)},
    {"paved_dark", new(0f, 1f, 0.5f)},
  };
  public static Color ParsePaint(string paint)
  {
    return ParsePaintColor(paint);
  }
  private static Color ParsePaintColor(string paint)
  {
    var split = Parse.Split(paint);
    if (split.Length < 3 && Paints.TryGetValue(paint, out var color)) return color;
    return new(Parse.Float(split, 0), Parse.Float(split, 1), Parse.Float(split, 2), Parse.Float(split, 3, 1f));
  }
  private static float CalculateSmooth(float smooth, float distance) => (1f - distance) >= smooth ? 1f : (1f - distance) / smooth;

  private static void GetHeightNodes(List<TerrainNode> nodes, Heightmap hm, Vector3 centerPos, float radius)
  {
    if (radius == 0f) return;
    var max = hm.m_width + 1;
    // Never return edges as they overlap with other zones.
    for (int x = 1; x < max - 1; x++)
    {
      for (int z = 1; z < max - 1; z++)
      {
        var nodePos = VertexToWorld(hm, x, z);
        var distance = Utils.DistanceXZ(centerPos, nodePos);
        if (distance > radius) continue;
        nodes.Add(new()
        {
          Index = z * max + x,
          Distance = distance / radius
        });
      }
    }
  }
  private static void GetPaintNodes(List<TerrainNode> nodes, Heightmap hm, Vector3 centerPos, float radius)
  {
    var max = hm.m_width;
    for (int x = 0; x < max; x++)
    {
      for (int z = 0; z < max; z++)
      {
        var nodePos = VertexToWorld(hm, x, z);
        // Painting is applied from the corner of the node, not the center.
        nodePos.x += 0.5f;
        nodePos.z += 0.5f;
        var distance = Utils.DistanceXZ(centerPos, nodePos);
        if (distance > radius) continue;
        nodes.Add(new()
        {
          Index = z * max + x,
          Distance = distance / radius
        });
      }
    }
  }

  private static Vector3 VertexToWorld(Heightmap hmap, int x, int y)
  {
    var vector = hmap.transform.position;
    vector.x += (x - hmap.m_width / 2 + 0.5f) * hmap.m_scale;
    vector.z += (y - hmap.m_width / 2 + 0.5f) * hmap.m_scale;
    return vector;
  }
  private static readonly int TerrainHash = "_TerrainCompiler".GetStableHashCode();
  public static void ChangeTerrain(Vector3 pos, Action<Heightmap, TerrainFakeData> action)
  {
    var hm = Heightmap.FindHeightmap(pos);
    if (hm == null) return;
    var compiler = FindCompiler(hm, pos);
    TerrainFakeData data = new(hm, compiler);
    action(hm, data);
    data.Save();
    var tc = TerrainComp.FindTerrainCompiler(pos);
    tc?.Load();
    hm.Poke(false);
  }
  private static ZDO FindCompiler(Heightmap hm, Vector3 pos)
  {
    var zone = ZoneSystem.instance.GetZone(pos);
    var index = ZDOMan.instance.SectorToIndex(zone);
    if (index < 0 || index >= ZDOMan.instance.m_objectsBySector.Length)
      return hm.GetAndCreateTerrainCompiler().m_nview.GetZDO();
    var tc = ZDOMan.instance.m_objectsBySector[index].FirstOrDefault(zdo => zdo.GetPrefab() == TerrainHash);
    if (tc == null)
      return hm.GetAndCreateTerrainCompiler().m_nview.GetZDO();
    return tc;
  }

  public static void Level(Heightmap hm, TerrainFakeData data, Vector3 pos, float radius, float border)
  {
    radius += border;
    if (radius == 0f) return;
    var smooth = border / radius;
    List<TerrainNode> nodes = [];
    GetHeightNodes(nodes, hm, pos, radius);
    void action(int index, float distance)
    {
      var multiplier = CalculateSmooth(smooth, distance);
      data.SetHeight(index, multiplier * (pos.y - hm.m_buildData.m_baseHeights[index]));
    }
    DoOperation(nodes, pos, radius, action);
  }
  public static void Paint(Heightmap hm, TerrainFakeData data, Vector3 pos, string paint, float radius, float border)
  {
    radius += border;
    if (radius == 0f) return;
    var smooth = border / radius;
    List<TerrainNode> nodes = [];
    Color color = ParsePaint(paint);
    GetPaintNodes(nodes, hm, pos, radius);
    void action(int index, float distance)
    {
      var multiplier = CalculateSmooth(smooth, distance);
      data.LerpColor(index, color, multiplier);
    }
    DoOperation(nodes, pos, radius, action);
  }

  private static void DoOperation(List<TerrainNode> nodes, Vector3 pos, float radius, Operation action)
  {
    foreach (var node in nodes)
      action(node.Index, node.Distance);
    ClutterSystem.instance?.ResetGrass(pos, radius);
  }
}

// TerrainCompiler is not loaded when Upgrade World resets locations.
// So using HeightMap.GetAndCreateTerrainCompiler would create a duplicate compiler.
// To avoid that, do operations directly on the ZDO data.
// This also ensures that default terrain state is used, so any previous operations won't affect the result.
public class TerrainFakeData
{
  public TerrainFakeData(Heightmap hm, ZDO zdo)
  {
    var num = hm.m_width + 1;
    HeightNodes = new HeightNode[num * num];
    PaintNodes = new Color[hm.m_width * hm.m_width];
    for (int i = 0; i < PaintNodes.Length; i++)
      PaintNodes[i] = Heightmap.m_paintMaskNothing;
    Zdo = zdo;
    Load(zdo.GetByteArray(ZDOVars.s_TCData));
  }
  private readonly ZDO Zdo;
  private int Operations = 0;
  private Vector3 LastOpPoint = Vector3.zero;
  private float LastOpRadius = 0f;
  private HeightNode[] HeightNodes = [];
  private Color[] PaintNodes = [];

  private struct HeightNode
  {
    public HeightNode()
    {
      LevelDelta = 0f;
      SmoothDelta = 0f;
    }
    public float LevelDelta = 0f;
    public float SmoothDelta = 0f;
  }
  public void SetHeight(int index, float value)
  {
    HeightNodes[index].LevelDelta = value;
    HeightNodes[index].SmoothDelta = 0f;
  }
  public void LerpColor(int index, Color color, float multiplier)
  {
    var newColor = Color.Lerp(PaintNodes[index], color, multiplier);
    newColor.a = color.a;
    PaintNodes[index] = newColor;
  }
  private void Load(byte[]? data)
  {
    if (data == null) return;
    ZPackage pkg = new(Utils.Decompress(data));
    pkg.ReadInt();
    Operations = pkg.ReadInt();
    LastOpPoint = pkg.ReadVector3();
    LastOpRadius = pkg.ReadSingle();
    var heightCount = pkg.ReadInt();
    HeightNodes = new HeightNode[heightCount];
    for (int i = 0; i < heightCount; i++)
    {
      var modified = pkg.ReadBool();
      if (!modified) continue;
      var levelDelta = pkg.ReadSingle();
      var smoothDelta = pkg.ReadSingle();
      HeightNodes[i] = new()
      {
        LevelDelta = levelDelta,
        SmoothDelta = smoothDelta
      };
    }
    var paintCount = pkg.ReadInt();
    PaintNodes = new Color[paintCount];
    for (int i = 0; i < paintCount; i++)
    {
      PaintNodes[i] = Heightmap.m_paintMaskNothing;
      var modified = pkg.ReadBool();
      if (!modified) continue;
      var r = pkg.ReadSingle();
      var g = pkg.ReadSingle();
      var b = pkg.ReadSingle();
      var a = pkg.ReadSingle();
      PaintNodes[i] = new(r, g, b, a);
    }
  }
  public void Save()
  {
    ZPackage pkg = new();
    pkg.Write(1);
    pkg.Write(Operations);
    pkg.Write(LastOpPoint);
    pkg.Write(LastOpRadius);
    pkg.Write(HeightNodes.Length);
    foreach (var node in HeightNodes)
    {
      var modified = node.LevelDelta != 0f || node.SmoothDelta != 0f;
      pkg.Write(modified);
      if (!modified) continue;
      pkg.Write(node.LevelDelta);
      pkg.Write(node.SmoothDelta);
    }
    pkg.Write(PaintNodes.Length);
    foreach (var node in PaintNodes)
    {
      var modified = node != Heightmap.m_paintMaskNothing;
      pkg.Write(modified);
      if (!modified) continue;
      pkg.Write(node.r);
      pkg.Write(node.g);
      pkg.Write(node.b);
      pkg.Write(node.a);
    }
    var bytes = Utils.Compress(pkg.GetArray());
    Zdo.Set(ZDOVars.s_TCData, bytes);
  }
}
