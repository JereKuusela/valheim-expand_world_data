using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData.Dungeon;
using Service;
using UnityEngine;

namespace ExpandWorldData;

public class RoomLoading
{
  public static string FileName = "expand_rooms.yaml";
  public static string FilePath = Path.Combine(Yaml.Directory, FileName);
  public static string Pattern = "expand_rooms*.yaml";


  private static List<DungeonDB.RoomData> DefaultEntries = [];


  // For finding rooms with wrong case.
  private static Dictionary<string, string> RoomNames = [];
  public static List<string> ParseRooms(string names) => DataManager.ToList(names).Select(s => RoomLoading.RoomNames.TryGetValue(s, out var name) ? name : s).ToList();
  public static void Initialize()
  {
    DefaultEntries.Clear();
    RoomSpawning.Prefabs.Clear();
    if (Helper.IsServer())
      SetDefaultEntries();
    Load();
  }
  private static void ToFile()
  {
    Save(DefaultEntries, false);
  }

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
    {"PlainsFortHildir", Room.Theme.PlainsFortHildir}
  };
  // For extra custom room themes.
  public static Dictionary<string, Room.Theme> NameToTheme = DefaultNameToTheme.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
  public static Dictionary<Room.Theme, string> ThemeToName = DefaultNameToTheme.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
  public static bool TryGetTheme(string name, out Room.Theme theme) => NameToTheme.TryGetValue(name.ToLowerInvariant(), out theme);

  public static void Load()
  {
    RoomSpawning.Data.Clear();
    DungeonObjects.Objects.Clear();
    DungeonObjects.ObjectSwaps.Clear();
    DungeonObjects.ObjectData.Clear();
    NameToTheme = DefaultNameToTheme.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);
    ThemeToName = DefaultNameToTheme.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    if (Helper.IsClient()) return;
    if (!Configuration.DataRooms)
    {
      Log.Info($"Reloading default room data ({DefaultEntries.Count} entries).");
      return;
    }
    if (!File.Exists(FilePath))
    {
      ToFile();
      return; // Watcher triggers reload.
    }
    var data = FromFile();
    if (data.Count == 0)
    {
      Log.Warning($"Failed to load any room data.");
      Log.Info($"Reloading default room data ({DefaultEntries.Count} entries).");
      return;
    }
    if (Configuration.DataMigration && AddMissingEntries(data))
    {
      // Watcher triggers reload.
      return;
    }
    Log.Info($"Reloading room themes ({NameToTheme.Count} entries).");
    Log.Info($"Reloading room data ({data.Count} entries).");

    RoomNames = data.ToDictionary(room => room.m_room.name.ToLowerInvariant(), room => room.m_room.name);
    DungeonDB.instance.m_rooms = data;
  }

  public static DungeonDB.RoomData CreateProxy(string name, string[] snapPieces)
  {
    DungeonDB.RoomData roomData = new();
    var room = new GameObject(name).AddComponent<Room>();
    roomData.m_room = room;
    if (RoomSpawning.Prefabs.TryGetValue(Parse.Name(name), out var baseRoom))
    {
      roomData.m_netViews = baseRoom.m_netViews;
      roomData.m_randomSpawns = baseRoom.m_randomSpawns;
      var connections = baseRoom.m_room.GetConnections();
      if (connections.Length != snapPieces.Length)
        Log.Warning($"Room {name} has {snapPieces.Length} connections, but base room {baseRoom.m_room.name} has {connections.Length} connections.");
      // Initialize with base connections to allow data missing from the yaml.
      room.m_roomConnections = connections.Select(c =>
        {
          var conn = new GameObject(c.name).AddComponent<RoomConnection>();
          conn.transform.parent = room.transform;
          conn.transform.localPosition = c.transform.localPosition;
          conn.transform.localRotation = c.transform.localRotation;
          conn.m_type = c.m_type;
          conn.m_entrance = c.m_entrance;
          conn.m_allowDoor = c.m_allowDoor;
          conn.m_doorOnlyIfOtherAlsoAllowsDoor = c.m_doorOnlyIfOtherAlsoAllowsDoor;
          return conn;
        }).ToArray();
    }
    else if (BlueprintManager.Load(name, snapPieces))
    {
      room.m_roomConnections = snapPieces.Select(c =>
        {
          var conn = new GameObject("").AddComponent<RoomConnection>();
          conn.transform.parent = room.transform;
          return conn;
        }).ToArray();
    }
    return roomData;
  }
  private static void UpdateConnections(Room room, RoomConnectionData[] data)
  {
    for (var i = 0; i < data.Length; ++i)
    {
      var connData = data[i];
      if (i >= room.m_roomConnections.Length)
      {
        var newConn = new GameObject(connData.type).AddComponent<RoomConnection>();
        newConn.transform.parent = room.transform;
        room.m_roomConnections = room.m_roomConnections.Append(newConn).ToArray();
      }
      var conn = room.m_roomConnections[i];
      if (conn.name == "")
        conn.name = connData.type;
      conn.m_type = connData.type;
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
  private static DungeonDB.RoomData FromData(RoomData data)
  {
    RoomSpawning.Data[data.name] = data;
    var snapPieces = data.connections.Select(c => c.position).ToArray();
    var roomData = CreateProxy(data.name, snapPieces);
    var room = roomData.m_room;
    var missingThemes = DataManager.ToList(data.theme).Where(s => !NameToTheme.ContainsKey(s.ToLowerInvariant())).ToArray();
    foreach (var theme in missingThemes)
    {
      var nextValue = (Room.Theme)(2 * (int)NameToTheme.Values.Max());
      NameToTheme[theme.ToLowerInvariant()] = nextValue;
      ThemeToName[nextValue] = theme;
    }
    room.m_theme = DataManager.ToEnum<Room.Theme>(data.theme);
    room.m_entrance = data.entrance;
    room.m_endCap = data.endCap;
    room.m_divider = data.divider;
    room.m_enabled = data.enabled;
    var size = Parse.VectorXZY(data.size);
    room.m_size = new((int)size.x, (int)size.y, (int)size.z);
    room.m_minPlaceOrder = data.minPlaceOrder;
    room.m_weight = data.weight;
    room.m_faceCenter = data.faceCenter;
    room.m_perimeter = data.perimeter;
    room.m_endCapPrio = data.endCapPriority;
    UpdateConnections(room, data.connections);
    if (data.objects != null)
      DungeonObjects.Objects[data.name] = Helper.ParseObjects(data.objects);
    if (data.objectSwap != null)
      DungeonObjects.ObjectSwaps[data.name] = Spawn.LoadSwaps(data.objectSwap);
    if (data.objectData != null)
      DungeonObjects.ObjectData[data.name] = Spawn.LoadData(data.objectData);
    return roomData;
  }
  private static RoomData ToData(DungeonDB.RoomData roomData)
  {
    var room = roomData.m_room;
    RoomData data = new()
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
      connections = room.GetConnections().Select(connection => new RoomConnectionData
      {
        position = $"{Helper.Print(connection.transform.localPosition)},{Helper.Print(connection.transform.localRotation)}",
        type = connection.m_type,
        entrance = connection.m_entrance,
        door = connection.m_allowDoor ? "true" : connection.m_doorOnlyIfOtherAlsoAllowsDoor ? "other" : "false"
      }).ToArray(),
    };
    return data;
  }

  private static List<DungeonDB.RoomData> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return Yaml.Deserialize<RoomData>(yaml, FileName).Select(FromData).Where(room => room.m_room).ToList();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
      Log.Error(e.StackTrace);
    }
    return [];
  }
  private static void SetDefaultEntries()
  {
    DefaultEntries = DungeonDB.instance.m_rooms.Where(room => room.m_room).ToList();
    RoomSpawning.Prefabs = DefaultEntries.ToDictionary(entry => entry.m_room.name, entry => entry);
  }
  private static bool AddMissingEntries(List<DungeonDB.RoomData> entries)
  {
    Dictionary<string, List<DungeonDB.RoomData>> perFile = [];
    var missingKeys = DefaultEntries.Select(entry => entry.m_room.name).Distinct().ToHashSet();
    foreach (var entry in entries)
      missingKeys.Remove(entry.m_room.name);
    if (missingKeys.Count == 0) return false;
    var missing = DefaultEntries.Where(entry => missingKeys.Contains(entry.m_room.name)).ToList();
    Log.Warning($"Adding {missing.Count} missing rooms to the expand_rooms.yaml file.");
    Save(missing, true);
    return true;
  }
  private static void Save(List<DungeonDB.RoomData> data, bool log)
  {
    Dictionary<string, List<DungeonDB.RoomData>> perFile = [];
    foreach (var item in data)
    {
      var mod = AssetTracker.GetModFromPrefab(item.m_room.name);
      var file = Configuration.SplitDataPerMod ? AssetTracker.GetFileNameFromMod(mod) : "";
      if (!perFile.ContainsKey(file))
        perFile[file] = [];
      perFile[file].Add(item);

      if (log)
        Log.Warning($"{mod}: {item.m_room.name}");
    }
    foreach (var kvp in perFile)
    {
      var file = Path.Combine(Yaml.Directory, $"expand_rooms{kvp.Key}.yaml");
      var yaml = File.Exists(file) ? File.ReadAllText(file) + "\n" : "";
      // Directly appending is risky but necessary to keep comments, etc.
      yaml += Yaml.Serializer().Serialize(kvp.Value.Select(ToData));
      File.WriteAllText(file, yaml);
    }
  }
  public static void SetupWatcher()
  {
    Yaml.SetupWatcher(Pattern, Load);
  }
}
