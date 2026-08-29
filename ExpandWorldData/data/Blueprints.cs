
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Service;
using UnityEngine;
using Data;
namespace ExpandWorldData;

public class BlueprintObject
{
  public string Prefab = "";
  public Vector3 Pos;
  public Quaternion Rot;
  public DataEntry? Data;
  public float Chance = 1f;
  public Vector3? Scale;
  public bool SnapToGround = false;
  public BlueprintObject(string name, Vector3 pos, Quaternion rot, Vector3 scale, DataEntry? data, float chance, bool snapToGround = false)
  {
    Prefab = name;
    Pos = pos;
    Rot = rot.normalized;
    Data = data;
    Chance = chance;
    SnapToGround = snapToGround;
    // Some blueprints have the scale only in the data, so don't override it with the default.
    if (scale != Vector3.one)
      Scale = scale;
  }
}

public class SnapPoint
{
  public Vector3 Pos;
  public Quaternion Rot;
  public SnapPoint(Vector3 pos, Quaternion rot)
  {
    Pos = pos;
    Rot = rot;
  }
}

public class BlueprintTerrain<T> where T : struct
{
  private const int MaxAxis = 2048;
  private const int MaxCells = 1024 * 1024;
  private readonly List<T?[]> Rows = [];
  private int ValueCount;

  public Vector3 CenterPosition;
  public Quaternion CenterRotation = Quaternion.identity;
  public float DistanceBetweenNodes = 1f;
  public int Width { get; private set; }
  public int Height => Rows.Count;
  public bool HasValues => ValueCount > 0;

  public void AddRow(T?[] values)
  {
    if (values.Length == 0 || values.Length > MaxAxis)
      throw new FormatException($"Terrain row width must be between 1 and {MaxAxis}.");
    if (Rows.Count >= MaxAxis || (long)values.Length * (Rows.Count + 1) > MaxCells)
      throw new FormatException($"Terrain grid can contain at most {MaxCells} cells.");
    if (Width == 0)
      Width = values.Length;
    else if (values.Length != Width)
      throw new FormatException($"Terrain row width {values.Length} does not match the first row width {Width}.");
    Rows.Add(values);
    ValueCount += values.Count(value => value.HasValue);
  }

  public T? Get(int x, int z)
  {
    if (x < 0 || x >= Width || z < 0 || z >= Height) return null;
    return Rows[z][x];
  }

  public void MapValues(Func<T, T> map)
  {
    foreach (var row in Rows)
    {
      for (var i = 0; i < row.Length; ++i)
      {
        var value = row[i];
        if (value.HasValue)
          row[i] = map(value.Value);
      }
    }
  }

  public bool AllValues(Func<T, bool> predicate)
  {
    foreach (var row in Rows)
    {
      foreach (var value in row)
      {
        if (value.HasValue && !predicate(value.Value)) return false;
      }
    }
    return true;
  }

  public void Center(Vector3 center, Quaternion rotation)
  {
    var referenceRotation = CenterRotation;
    CenterPosition -= referenceRotation * center;
    CenterRotation = Yaw(referenceRotation * Quaternion.Inverse(rotation));
  }

  public float GetRadius()
  {
    if (Width == 0 || Height == 0) return 0f;
    var spacing = Mathf.Max(0.001f, DistanceBetweenNodes);
    var firstNode = CenterPosition - new Vector3((Width - 1) * spacing * 0.5f, 0f, (Height - 1) * spacing * 0.5f);
    var radius = 0f;
    for (var z = 0; z < Height; ++z)
    {
      for (var x = 0; x < Width; ++x)
      {
        if (!Get(x, z).HasValue) continue;
        var node = firstNode + new Vector3(x * spacing, 0f, z * spacing);
        radius = Mathf.Max(radius, Utils.LengthXZ(node));
      }
    }
    return radius;
  }

  public T? FindNearest(Vector3 nodePosition, Vector3 placementPosition, Quaternion placementRotation)
  {
    if (Width == 0 || Height == 0) return null;
    var relativeRotation = Yaw(placementRotation * Quaternion.Inverse(CenterRotation));
    var localPosition = Quaternion.Inverse(relativeRotation) * (nodePosition - placementPosition);
    var spacing = Mathf.Max(0.001f, DistanceBetweenNodes);
    var firstNode = CenterPosition - new Vector3((Width - 1) * spacing * 0.5f, 0f, (Height - 1) * spacing * 0.5f);
    var x = Mathf.RoundToInt((localPosition.x - firstNode.x) / spacing);
    var z = Mathf.RoundToInt((localPosition.z - firstNode.z) / spacing);
    return Get(x, z);
  }

  private static Quaternion Yaw(Quaternion rotation)
  {
    var forwardX = 2.0 * (rotation.x * rotation.z + rotation.w * rotation.y);
    var forwardZ = 1.0 - 2.0 * (rotation.x * rotation.x + rotation.y * rotation.y);
    if (forwardX * forwardX + forwardZ * forwardZ <= 0.0001) return Quaternion.identity;
    var halfYaw = Math.Atan2(forwardX, forwardZ) * 0.5;
    return new(0f, (float)Math.Sin(halfYaw), 0f, (float)Math.Cos(halfYaw));
  }
}

public class Blueprint
{
  // A square with this half-extent can touch at most 8 x 8 terrain zones.
  // Keeping the parser-side bound aligned with Terrain.ApplyBlueprint avoids
  // letting blueprint radius calculations reach ZoneSystem with huge values.
  private const float MaxTerrainApplicationRadius = ZoneSystem.c_ZoneSize * 3.5f;
  public string Name;
  public List<BlueprintObject> Objects = [];
  public string CenterPiece = "piece_bpcenterpoint";
  public float Radius = 0f;
  public List<SnapPoint> SnapPoints = [];
  public Vector3 Size = Vector3.one;
  public BlueprintTerrain<float>? TerrainHeight;
  public BlueprintTerrain<Color>? TerrainPaint;

  public Blueprint(string name)
  {
    Name = name;
  }
  private void AddSnapPoint(Vector3 pos, Quaternion rot, int index)
  {
    if (SnapPoints.Count <= index)
      SnapPoints.Add(new(pos, rot));
    else
      SnapPoints[index] = new(pos, rot);
  }
  // Provides a way to override or load snap points from pieces or coordinates.
  // Snap point system is used for dungeon room connections.
  public void LoadSnapPoints(string[] snapPieces)
  {
    for (var i = 0; i < snapPieces.Length; ++i)
    {
      var piece = snapPieces[i];
      if (piece.Split(',').Length == 3)
      {
        var pos = Parse.VectorXZY(piece);
        AddSnapPoint(pos, Quaternion.identity, i);
        continue;
      }
      var success = false;
      foreach (var obj in Objects)
      {
        if (obj.Prefab != piece) continue;
        if (obj.Chance == 0f) continue;
        obj.Chance = 0f;
        AddSnapPoint(obj.Pos, obj.Rot, i);
        success = true;
        break;
      }
      if (!success)
        Log.Warning($"Snap point piece {piece} not found in blueprint {Name}.");
    }
  }
  public void Center()
  {
    Bounds bounds = new();
    var hasObjects = Objects.Count > 0;
    var y = hasObjects ? float.MaxValue : 0f;
    Quaternion rot = Quaternion.identity;
    foreach (var obj in Objects)
    {
      y = Mathf.Min(y, obj.Pos.y);
      bounds.Encapsulate(obj.Pos);
    }
    // Slightly towards the ground to prevent gaps.
    if (hasObjects) y += 0.05f;
    Size = hasObjects ? bounds.size : Vector3.zero;
    Vector3 center = hasObjects ? new(bounds.center.x, y, bounds.center.z) : Vector3.zero;
    foreach (var obj in Objects)
    {
      if (obj.Prefab == CenterPiece)
      {
        center = obj.Pos;
        rot = Quaternion.Inverse(obj.Rot);
        // Bit hacky way to prevent it from being spawned.
        obj.Chance = 0f;
        break;
      }
    }
    Radius = Utils.LengthXZ(bounds.extents);
    foreach (var obj in Objects)
      obj.Pos -= center;
    if (TerrainHeight != null)
    {
      TerrainHeight.MapValues(value => value - center.y);
      TerrainHeight.Center(center, rot);
    }
    if (TerrainPaint != null)
      TerrainPaint.Center(center, rot);
    if (rot != Quaternion.identity)
    {
      foreach (var obj in Objects)
      {
        obj.Pos = rot * obj.Pos;
        obj.Rot = rot * obj.Rot;
      }
    }
    if (TerrainHeight != null && !IsValidTerrain(TerrainHeight, IsFinite))
    {
      Log.Warning($"Blueprint {Name}: Ignoring an invalid or oversized terrain height snapshot.");
      TerrainHeight = null;
    }
    if (TerrainPaint != null && !IsValidTerrain(TerrainPaint, IsFinite))
    {
      Log.Warning($"Blueprint {Name}: Ignoring an invalid or oversized terrain paint snapshot.");
      TerrainPaint = null;
    }
    var terrainRadius = Mathf.Max(TerrainHeight?.GetRadius() ?? 0f, TerrainPaint?.GetRadius() ?? 0f);
    var terrainMargin = Mathf.Max(1f,
      TerrainHeight?.HasValues == true ? TerrainHeight.DistanceBetweenNodes : 0f,
      TerrainPaint?.HasValues == true ? TerrainPaint.DistanceBetweenNodes : 0f);
    if (TerrainHeight?.HasValues == true && TerrainPaint?.HasValues == true &&
        (!IsFinite(terrainRadius + terrainMargin) || terrainRadius + terrainMargin > MaxTerrainApplicationRadius))
    {
      Log.Warning($"Blueprint {Name}: Ignoring terrain snapshots whose combined bounds cover too many zones.");
      TerrainHeight = null;
      TerrainPaint = null;
      terrainRadius = 0f;
    }
    Radius = Mathf.Max(Radius, terrainRadius);
  }

  private static bool IsValidTerrain<T>(BlueprintTerrain<T> terrain, Func<T, bool> valueIsFinite) where T : struct
  {
    var rotationMagnitude = terrain.CenterRotation.x * terrain.CenterRotation.x +
      terrain.CenterRotation.y * terrain.CenterRotation.y +
      terrain.CenterRotation.z * terrain.CenterRotation.z +
      terrain.CenterRotation.w * terrain.CenterRotation.w;
    if (!IsFinite(terrain.CenterPosition) || !IsFinite(terrain.CenterRotation) ||
        !IsFinite(rotationMagnitude) || rotationMagnitude <= 0.0001f ||
        !IsFinite(terrain.DistanceBetweenNodes) || terrain.DistanceBetweenNodes <= 0f ||
        !terrain.AllValues(valueIsFinite))
      return false;
    var radius = terrain.GetRadius();
    var applicationRadius = radius + Mathf.Max(1f, terrain.DistanceBetweenNodes);
    return IsFinite(radius) && radius >= 0f && IsFinite(applicationRadius) &&
      applicationRadius <= MaxTerrainApplicationRadius;
  }

  private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
  private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
  private static bool IsFinite(Quaternion value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
  private static bool IsFinite(Color value) => IsFinite(value.r) && IsFinite(value.g) && IsFinite(value.b) && IsFinite(value.a);
}
public class Blueprints
{
  private static IEnumerable<string> LoadFiles(string folder, IEnumerable<string> bps)
  {
    if (Directory.Exists(folder))
    {
      var blueprints = Directory.EnumerateFiles(folder, "*.blueprint", SearchOption.AllDirectories);
      var vbuilds = Directory.EnumerateFiles(folder, "*.vbuild", SearchOption.AllDirectories);
      return bps.Concat(blueprints).Concat(vbuilds);
    }
    return bps;
  }
  private static IEnumerable<string> Files()
  {
    IEnumerable<string> bps = [];
    bps = LoadFiles(Configuration.BlueprintGlobalFolder, bps);
    if (Path.GetFullPath(Configuration.BlueprintLocalFolder) != Path.GetFullPath(Configuration.BlueprintGlobalFolder))
      bps = LoadFiles(Configuration.BlueprintLocalFolder, bps);
    return bps.Distinct().OrderBy(s => s);
  }
  public static bool TryGetBluePrint(string name, out Blueprint blueprint)
  {
    blueprint = new("Invalid");
    var bp = GetBluePrint(name);
    if (bp == null) return false;
    blueprint = bp;
    return true;
  }
  public static Blueprint? GetBluePrint(string name)
  {
    name = name.Replace(" ", "_");
    var path = Files().FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Replace(" ", "_") == name);
    if (path == null) return null;
    var rows = File.ReadAllLines(path);
    var extension = Path.GetExtension(path);
    Blueprint bp = new(name);
    if (extension == ".vbuild") return GetBuildShare(bp, rows);
    if (extension == ".blueprint") return GetPlanBuild(bp, rows);
    throw new InvalidOperationException("Unknown file format.");
  }
  private static Blueprint? GetPlanBuild(Blueprint bp, string[] rows)
  {
    var section = PlanBuildSection.Pieces;
    var currentRow = -1;
    try
    {
      foreach (var row in rows)
      {
        currentRow += 1;
        if (row.StartsWith("#", StringComparison.Ordinal))
        {
          section = ReadPlanBuildHeader(bp, row);
          continue;
        }
        if (section == PlanBuildSection.Pieces)
          bp.Objects.Add(GetPlanBuildObject(row, bp.Name));
        else if (section == PlanBuildSection.SnapPoints)
          bp.SnapPoints.Add(new(GetPlanBuildSnapPoint(row), Quaternion.identity));
        else if (section == PlanBuildSection.TerrainHeight && bp.TerrainHeight != null)
          bp.TerrainHeight.AddRow(GetPlanBuildHeightRow(row));
        else if (section == PlanBuildSection.TerrainPaint && bp.TerrainPaint != null)
          bp.TerrainPaint.AddRow(GetPlanBuildPaintRow(row));
      }
    }
    catch (Exception e)
    {
      Log.Error($"Failed to load blueprint {bp.Name} at row {currentRow}: {rows[currentRow]}. {e.Message}");
      return null;
    }
    return bp;
  }

  private enum PlanBuildSection
  {
    Ignore,
    SnapPoints,
    Pieces,
    TerrainHeight,
    TerrainPaint
  }

  private static PlanBuildSection ReadPlanBuildHeader(Blueprint bp, string row)
  {
    var separator = row.IndexOf(':');
    var name = (separator < 0 ? row : row.Substring(0, separator)).Trim();
    if (name.Equals("#snappoints", StringComparison.OrdinalIgnoreCase))
      return PlanBuildSection.SnapPoints;
    if (name.Equals("#pieces", StringComparison.OrdinalIgnoreCase))
      return PlanBuildSection.Pieces;
    if (name.Equals("#center", StringComparison.OrdinalIgnoreCase))
    {
      if (separator < 0) throw new FormatException("#Center requires a prefab name.");
      bp.CenterPiece = row.Substring(separator + 1);
      return PlanBuildSection.Ignore;
    }
    if (name.Equals("#terrainheight", StringComparison.OrdinalIgnoreCase))
    {
      try
      {
        bp.TerrainHeight = GetPlanBuildTerrain<float>(row, separator);
        return PlanBuildSection.TerrainHeight;
      }
      catch (FormatException)
      {
        return PlanBuildSection.Ignore;
      }
      catch (OverflowException)
      {
        return PlanBuildSection.Ignore;
      }
    }
    if (name.Equals("#terrainpaint", StringComparison.OrdinalIgnoreCase))
    {
      try
      {
        bp.TerrainPaint = GetPlanBuildTerrain<Color>(row, separator);
        return PlanBuildSection.TerrainPaint;
      }
      catch (FormatException)
      {
        return PlanBuildSection.Ignore;
      }
      catch (OverflowException)
      {
        return PlanBuildSection.Ignore;
      }
    }
    // Unknown sections must not inherit the previous parser state. This keeps
    // future PlanBuild extensions from being interpreted as pieces.
    return PlanBuildSection.Ignore;
  }

  private static BlueprintTerrain<T> GetPlanBuildTerrain<T>(string row, int separator) where T : struct
  {
    if (separator < 0) throw new FormatException("Terrain section requires center, rotation and node spacing.");
    var split = row.Substring(separator + 1).Split(';');
    if (split.Length < 3) throw new FormatException("Terrain section requires center, rotation and node spacing.");
    var center = split[0].Split(',');
    if (center.Length != 3) throw new FormatException("Terrain center requires X, Z and Y coordinates.");
    var centerX = RequiredInvariantFloat(center, 0);
    var centerZ = RequiredInvariantFloat(center, 1);
    var centerY = RequiredInvariantFloat(center, 2);
    var yaw = RequiredInvariantFloat(split, 1);
    var spacing = RequiredInvariantFloat(split, 2);
    if (!IsFinite(centerX) || !IsFinite(centerY) || !IsFinite(centerZ) || !IsFinite(yaw))
      throw new FormatException("Terrain center and rotation must contain finite numbers.");
    if (!IsFinite(spacing) || spacing <= 0f)
      throw new FormatException("Terrain node spacing must be a finite positive number.");
    return new()
    {
      CenterPosition = new(centerX, centerY, centerZ),
      CenterRotation = PlanBuildYaw(yaw),
      DistanceBetweenNodes = spacing
    };
  }

  private static Quaternion PlanBuildYaw(float degrees)
  {
    var radians = degrees * Math.PI / 180.0 * 0.5;
    return new(0f, (float)Math.Sin(radians), 0f, (float)Math.Cos(radians));
  }

  private static float?[] GetPlanBuildHeightRow(string row)
  {
    if (row.IndexOf(',') > -1) row = row.Replace(',', '.');
    var split = row.Split(';');
    var values = new float?[split.Length];
    for (var i = 0; i < split.Length; ++i)
    {
      if (!string.IsNullOrEmpty(split[i]))
      {
        var value = InvariantFloat(split, i);
        if (!IsFinite(value)) throw new FormatException("Terrain height values must be finite numbers.");
        values[i] = value;
      }
    }
    return values;
  }

  private static Color?[] GetPlanBuildPaintRow(string row)
  {
    if (row.IndexOf(',') > -1) row = row.Replace(',', '.');
    var split = row.Split(';');
    var values = new Color?[split.Length];
    for (var i = 0; i < split.Length; ++i)
    {
      if (string.IsNullOrEmpty(split[i])) continue;
      var color = split[i].Split(':');
      if (color.Length < 3) throw new FormatException($"Terrain paint value '{split[i]}' requires r:g:b[:a].");
      var r = RequiredInvariantFloat(color, 0);
      var g = RequiredInvariantFloat(color, 1);
      var b = RequiredInvariantFloat(color, 2);
      var a = color.Length > 3 ? RequiredInvariantFloat(color, 3) : 1f;
      if (!IsFinite(r) || !IsFinite(g) || !IsFinite(b) || !IsFinite(a))
        throw new FormatException("Terrain paint values must be finite numbers.");
      values[i] = new(r, g, b, a);
    }
    return values;
  }
  private static BlueprintObject GetPlanBuildObject(string row, string fileName)
  {
    if (row.IndexOf(',') > -1) row = row.Replace(',', '.');
    var split = row.Split(';');
    var name = split[0];
    var posX = InvariantFloat(split, 2);
    var posY = InvariantFloat(split, 3);
    var posZ = InvariantFloat(split, 4);
    var rotX = InvariantFloat(split, 5);
    var rotY = InvariantFloat(split, 6);
    var rotZ = InvariantFloat(split, 7);
    var rotW = InvariantFloat(split, 8);
    // Info is not supported.
    var scaleX = InvariantFloat(split, 10, 1f);
    var scaleY = InvariantFloat(split, 11, 1f);
    var scaleZ = InvariantFloat(split, 12, 1f);
    var data = split.Length > 13 ? split[13] : "";
    var chance = InvariantFloat(split, 14, 1f);
    return new(name, new(posX, posY, posZ), new(rotX, rotY, rotZ, rotW), new(scaleX, scaleY, scaleZ), DataHelper.Get(data, fileName), chance);
  }
  private static Vector3 GetPlanBuildSnapPoint(string row)
  {
    if (row.IndexOf(',') > -1) row = row.Replace(',', '.');
    var split = row.Split(';');
    var x = InvariantFloat(split, 0);
    var y = InvariantFloat(split, 1);
    var z = InvariantFloat(split, 2);
    return new(x, y, z);
  }
  private static Blueprint GetBuildShare(Blueprint bp, string[] rows)
  {
    bp.Objects = rows.Select(r => GetBuildShareObject(r, bp.Name)).ToList();
    return bp;
  }
  private static BlueprintObject GetBuildShareObject(string row, string fileName)
  {
    if (row.IndexOf(',') > -1) row = row.Replace(',', '.');
    var split = row.Split(' ');
    var name = split[0];
    var rotX = InvariantFloat(split, 1);
    var rotY = InvariantFloat(split, 2);
    var rotZ = InvariantFloat(split, 3);
    var rotW = InvariantFloat(split, 4);
    var posX = InvariantFloat(split, 5);
    var posY = InvariantFloat(split, 6);
    var posZ = InvariantFloat(split, 7);
    var data = split.Length > 8 ? split[8] : "";
    var chance = split.Length > 9 ? InvariantFloat(split, 9, 1f) : 1f;
    return new(name, new(posX, posY, posZ), new(rotX, rotY, rotZ, rotW), Vector3.one, DataHelper.Get(data, fileName), chance);
  }
  private static float InvariantFloat(string[] row, int index, float defaultValue = 0f)
  {
    if (index >= row.Length) return defaultValue;
    var s = row[index];
    if (string.IsNullOrEmpty(s)) return defaultValue;
    return float.Parse(s, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
  }
  private static float RequiredInvariantFloat(string[] row, int index)
  {
    if (index >= row.Length || string.IsNullOrWhiteSpace(row[index]))
      throw new FormatException("A required terrain number is missing.");
    return float.Parse(row[index], NumberStyles.Any, NumberFormatInfo.InvariantInfo);
  }
  private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
