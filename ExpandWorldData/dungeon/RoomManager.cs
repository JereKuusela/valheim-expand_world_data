using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Dungeon;
using Service;
using UnityEngine;
using Common;

namespace ExpandWorldData;

public class RoomManager
{
  public static string FileName = "expand_rooms.yaml";
  public static string FilePath = Path.Combine(Yaml.BaseDirectory, FileName);
  public static string Pattern = "expand_rooms*.yaml";
  private static readonly List<RoomYaml> ExtraRoomYamls = [];

  private class Config : YamlDataManager
  {
    protected override string FilePath => RoomManager.FilePath;
    protected override string Pattern => RoomManager.Pattern;
    protected override bool DataEnabled => Configuration.DataRooms;
    protected override SetResult Set(Dictionary<string, string> files) => RoomManager.Set(files);
    protected override void WriteDefaultConfig() => ToFile();
  }
  private static readonly Config Instance = new();

  public static void AddRoom(RoomYaml yaml)
  {
    ExtraRoomYamls.Add(yaml);
  }


  private static List<DungeonDB.RoomData> DefaultEntries = [];


  // For finding rooms with wrong case.
  private static Dictionary<string, string> RoomNames = [];
  public static List<string> ParseRooms(string names) => DataManager.ToList(names).Select(s => RoomNames.TryGetValue(s, out var name) ? name : s).ToList();
  public static void Initialize()
  {
    DefaultEntries.Clear();
    RoomSpawning.Prefabs.Clear();
    CreatedObjects.ForEach(UnityEngine.Object.Destroy);
    ClearData();
    if (Helper.IsServer())
      SetDefaultEntries();
    ReadConfigs();
  }
  private static void ToFile()
  {
    var data = DefaultEntries.Select(ToData).Where(d => d != null).ToList();
    if (ExtraRoomYamls.Count > 0)
      data.AddRange(ExtraRoomYamls);
    Save(data, false);
  }

  // Hard coded to not go through the enum patch.
  private static readonly Dictionary<string, Room.Theme> DefaultNameToTheme = new() {
    {"Crypt", Room.Theme.Crypt},
    {"SunkenCrypt", Room.Theme.SunkenCrypt},
    {"Cave", Room.Theme.Cave},
    {"ForestCrypt", Room.Theme.ForestCrypt},
    {"GoblinCamp", Room.Theme.GoblinCamp},
    {"MeadowsVillage", Room.Theme.MeadowsVillage},
    {"MeadowsFarm", Room.Theme.MeadowsFarm},
    {"DvergerTown", Room.Theme.DvergerTown},
    {"DvergerBoss", Room.Theme.DvergerBoss},
    {"ForestCryptHildir", Room.Theme.ForestCryptHildir},
    {"CaveHildir", Room.Theme.CaveHildir},
    {"PlainsFortHildir", Room.Theme.PlainsFortHildir},
    {"AshlandRuins", Room.Theme.AshlandRuins},
    {"FortressRuins", Room.Theme.FortressRuins},
    {"Hole", Room.Theme.Hole},
    {"NorthVillage", Room.Theme.NorthVillage},
    {"MorkHalla", Room.Theme.MorkHalla}
  };

  // For extra custom room themes.
  private static Dictionary<string, Room.Theme> CreateNameToTheme()
  {
    var themes = DefaultNameToTheme.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
    foreach (var theme in themes.Values.Distinct().ToArray())
      themes[((int)theme).ToString()] = theme;
    return themes;
  }

  public static Dictionary<string, Room.Theme> NameToTheme = CreateNameToTheme();
  public static Dictionary<Room.Theme, string> ThemeToName = DefaultNameToTheme.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
  public static bool TryGetTheme(string name, out Room.Theme theme) => NameToTheme.TryGetValue(name.ToLowerInvariant(), out theme);

  public static void ReadConfigs() => Instance.ReadConfigs();

  private static void ClearData()
  {
    RoomSpawning.Data = [];
    RoomSpawning.Blueprints = [];
    RoomSpawning.RoomSizes = [];
    DungeonObjects.Objects = [];
    DungeonObjects.ObjectSwaps = [];
    DungeonObjects.ObjectData = [];
    CreatedObjects = [];
    NameToTheme = CreateNameToTheme();
    ThemeToName = DefaultNameToTheme.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
  }

  private static SetResult Set(Dictionary<string, string> files)
  {
    var previousData = RoomSpawning.Data;
    var previousBlueprints = RoomSpawning.Blueprints;
    var previousSizes = RoomSpawning.RoomSizes;
    var previousObjects = DungeonObjects.Objects;
    var previousSwaps = DungeonObjects.ObjectSwaps;
    var previousObjectData = DungeonObjects.ObjectData;
    var previousCreated = CreatedObjects;
    var previousNameToTheme = NameToTheme;
    var previousThemeToName = ThemeToName;
    ClearData();
    var result = SetResult.Failed;
    try
    {
      return result = LoadAndApply(files);
    }
    finally
    {
      if (result == SetResult.Applied)
        previousCreated.ForEach(UnityEngine.Object.Destroy);
      else
      {
        CreatedObjects.ForEach(UnityEngine.Object.Destroy);
        RoomSpawning.Data = previousData;
        RoomSpawning.Blueprints = previousBlueprints;
        RoomSpawning.RoomSizes = previousSizes;
        DungeonObjects.Objects = previousObjects;
        DungeonObjects.ObjectSwaps = previousSwaps;
        DungeonObjects.ObjectData = previousObjectData;
        CreatedObjects = previousCreated;
        NameToTheme = previousNameToTheme;
        ThemeToName = previousThemeToName;
      }
    }
  }

  private static SetResult LoadAndApply(Dictionary<string, string> files)
  {
    List<DungeonDB.RoomData> data = [];
    if (files.Count == 0)
    {
      data = DefaultEntries;
      Log.Info($"Reloading default room data ({DefaultEntries.Count} entries).");
    }
    else
    {
      foreach (var file in files)
      {
        if (!Yaml.TryDeserialize<RoomYaml>(file.Value, file.Key, out var parsed)) return SetResult.Failed;
        data.AddRange(parsed.Select(item => FromData(item, file.Key)).Where(room => room != null && room.m_prefab != null && !string.IsNullOrWhiteSpace(room.m_prefab.m_name)));
      }
    }
    if (data.Count == 0)
    {
      Log.Warning("Failed to load any room data.");
      return SetResult.Failed;
    }
    if (files.Count > 0 && YamlDataManager.BackfillEnabled && AddMissingEntries(data))
      return SetResult.Rewritten;
    Log.Info($"Reloading room themes ({NameToTheme.Count} entries).");
    Log.Info($"Reloading room data ({data.Count} entries).");

    RoomNames = Helper.ToDict(data, item => item.m_prefab.Name.ToLowerInvariant(), item => item.m_prefab.Name);
    DungeonDB.instance.m_rooms = data;
    return SetResult.Applied;
  }
  private static List<GameObject> CreatedObjects = [];
  public static DungeonDB.RoomData CreateRoomData(string name, string[] snapPieces)
  {
    DungeonDB.RoomData roomData = new();
    var room = new GameObject(name).AddComponent<Room>();
    CreatedObjects.Add(room.gameObject);
    roomData.m_loadedRoom = room;
    var baseName = Parse.Name(name);
    if (RoomSpawning.Prefabs.TryGetValue(baseName, out var baseRoom))
    {
      // Variants need to load the base room to get the connections.
      roomData.m_prefab = baseRoom.m_prefab;
      var asset = Helper.SafeLoad(baseRoom);
      if (asset == null)
        return roomData;
      room.m_musicPrefab = baseRoom.RoomInPrefab.m_musicPrefab;
      var connections = baseRoom.RoomInPrefab.GetConnections();
      if (connections.Length != snapPieces.Length)
        Log.Warning($"Room {name} has {snapPieces.Length} connections, but base room {baseRoom.m_prefab.Name} has {connections.Length} connections.");
      // Initialize with base connections to allow data missing from the yaml.
      room.m_roomConnections = [.. connections.Select(c =>
        {
          var conn = new GameObject(c.name).AddComponent<RoomConnection>();
          CreatedObjects.Add(conn.gameObject);
          conn.transform.parent = room.transform;
          conn.transform.localPosition = c.transform.localPosition;
          conn.transform.localRotation = c.transform.localRotation;
          conn.m_type = c.m_type;
          conn.m_entrance = c.m_entrance;
          conn.m_allowDoor = c.m_allowDoor;
          conn.m_doorOnlyIfOtherAlsoAllowsDoor = c.m_doorOnlyIfOtherAlsoAllowsDoor;
          return conn;
        })];
      baseRoom.m_prefab.Release();
    }
    else if (BlueprintManager.Load(baseName, snapPieces))
    {
      RoomSpawning.Blueprints[roomData] = baseName;
      roomData.m_prefab = new(new())
      {
        m_name = name,
        m_loadedAsset = room.gameObject
      };
      room.m_roomConnections = [.. snapPieces.Select(c =>
      {
        var conn = new GameObject("").AddComponent<RoomConnection>();
        CreatedObjects.Add(conn.gameObject);
        conn.transform.parent = room.transform;
        return conn;
      })];
    }
    return roomData;
  }
  private static void UpdateConnections(Room room, RoomConnectionYaml[] data)
  {
    for (var i = 0; i < data.Length; ++i)
    {
      var connData = data[i];
      if (i >= room.m_roomConnections.Length)
      {
        var newConn = new GameObject(connData.type).AddComponent<RoomConnection>();
        CreatedObjects.Add(newConn.gameObject);
        newConn.transform.parent = room.transform;
        room.m_roomConnections = room.m_roomConnections.Append(newConn).ToArray();
      }
      var conn = room.m_roomConnections[i];
      if (conn.name == "")
        conn.name = connData.type;
      conn.m_type = connData.exitOnly ? $">{connData.type}" : connData.type;
      conn.m_entrance = connData.entrance;
      conn.m_allowDoor = connData.door == "true";
      conn.m_doorOnlyIfOtherAlsoAllowsDoor = connData.door == "other";
      // Old yamls won't have rotation set so this is important to keep them working.
      // Also it doesn't hurt that the position can be missing as well.
      var split = Parse.Split(connData.position);
      if (split.Length > 2)
        conn.transform.localPosition = Parse.VectorXZY(split, 0);
      if (split.Length > 3)
        conn.transform.localRotation = Parse.AngleYXZ(split, 3);
    }
  }
  private static DungeonDB.RoomData FromData(RoomYaml data, string fileName)
  {
    var snapPieces = data.connections.Select(c => c.position).ToArray();
    var roomData = CreateRoomData(data.name, snapPieces);
    RoomSpawning.Data[roomData] = data;
    var room = roomData.RoomInPrefab;
    var missingThemes = DataManager.ToList(data.theme).Where(s => !NameToTheme.ContainsKey(s.ToLowerInvariant())).ToArray();
    var maxValue = NameToTheme.Values.Max(value => (int)value);
    // Note: Parsing of numeric values not supported, would be quite complex with multiple files.
    foreach (var theme in missingThemes)
    {
      if (NameToTheme.ContainsValue((Room.Theme)int.MinValue))
        throw new InvalidOperationException($"Cannot create room theme '{theme}': the maximum theme value has been reached.");
      maxValue *= 2;
      var maxTheme = (Room.Theme)maxValue;
      NameToTheme[theme.ToLowerInvariant()] = maxTheme;
      NameToTheme[maxValue.ToString()] = maxTheme;
      ThemeToName[maxTheme] = theme;
    }
    room.m_theme = DataManager.ToEnum<Room.Theme>(data.theme);
    room.m_entrance = data.entrance;
    room.m_endCap = data.endCap;
    room.m_divider = data.divider;
    room.m_enabled = data.enabled;
    var size = Parse.VectorXZY(data.size);
    RoomSpawning.RoomSizes[room] = size;
    room.m_size = new((int)size.x, (int)size.y, (int)size.z);
    room.m_minPlaceOrder = data.minPlaceOrder;
    room.m_weight = data.weight;
    room.m_faceCenter = data.faceCenter;
    room.m_perimeter = data.perimeter;
    room.m_endCapPrio = data.endCapPriority;
    roomData.m_theme = room.m_theme;
    roomData.m_enabled = room.m_enabled;
    UpdateConnections(room, data.connections);
    if (data.objects != null)
      DungeonObjects.Objects[roomData] = Helper.ParseObjects(data.objects, fileName);
    if (data.objectSwap != null)
      DungeonObjects.ObjectSwaps[roomData] = Spawn.LoadSwaps(data.objectSwap);
    if (data.objectData != null)
      DungeonObjects.ObjectData[roomData] = Spawn.LoadData(data.objectData, fileName);
    return roomData;
  }
  private static RoomYaml ToData(DungeonDB.RoomData roomData)
  {
    var asset = Helper.SafeLoad(roomData);
    if (asset == null)
      return null!;
    var room = roomData.RoomInPrefab;
    RoomYaml data = new()
    {
      name = room.gameObject.name,
      theme = DataManager.FromEnum(room.m_theme),
      entrance = room.m_entrance,
      endCap = room.m_endCap,
      divider = room.m_divider,
      enabled = room.m_enabled,
      size = $"{room.m_size.x},{room.m_size.z},{room.m_size.y}",
      minPlaceOrder = room.m_minPlaceOrder,
      weight = room.m_weight,
      faceCenter = room.m_faceCenter,
      perimeter = room.m_perimeter,
      endCapPriority = room.m_endCapPrio,
      connections = room.GetConnections().Select(connection => new RoomConnectionYaml
      {
        position = $"{Helper.Print(connection.transform.localPosition)},{Helper.Print(connection.transform.localRotation)}",
        type = connection.m_type,
        entrance = connection.m_entrance,
        door = connection.m_allowDoor ? "true" : connection.m_doorOnlyIfOtherAlsoAllowsDoor ? "other" : "false"
      }).ToArray(),
    };
    roomData.m_prefab.Release();
    return data;
  }

  private static void SetDefaultEntries()
  {
    DefaultEntries = DungeonDB.instance.m_rooms.Where(room => room.m_prefab.IsValid).ToList();
    RoomSpawning.Prefabs = DefaultEntries.ToDictionary(entry => entry.m_prefab.Name, entry => entry);
  }
  private static bool AddMissingEntries(List<DungeonDB.RoomData> entries)
  {
    var missingKeys = DefaultEntries.Select(entry => entry.m_prefab.Name).Distinct().ToHashSet();
    foreach (var entry in entries)
      missingKeys.Remove(entry.m_prefab.Name);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(entry => missingKeys.Contains(entry.m_prefab.Name)).Select(ToData).Where(d => d != null).ToList();
    Log.Warning($"Adding {missing.Count} missing rooms to the expand_rooms.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<RoomYaml> data, bool log)
  {
    YamlFiles.SavePerMod("rooms", data, item => item.name, log);
  }
  public static void SetupWatcher()
  {
    Yaml.SetupDataWatcher(Pattern, Configuration.configDataRooms, ReadConfigs);
  }
}
