using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorldData;
public class DebugCommands
{
  private string GetRoomItems(DungeonDB.RoomData room)
  {
    room.m_prefab.Load();
    var items = Utils.GetEnabledComponentsInChildren<ZNetView>(room.m_prefab.Asset).Select(netView => Utils.GetPrefabName(netView.gameObject)).GroupBy(name => name).Select(group => group.Key + " x" + group.Count());
    room.m_prefab.Release();
    return string.Join(", ", items);
  }

  private string GetLocationItems(ZoneSystem.ZoneLocation loc)
  {
    loc.m_prefab.Load();
    var items = Utils.GetEnabledComponentsInChildren<ZNetView>(loc.m_prefab.Asset).Select(netView => Utils.GetPrefabName(netView.gameObject)).GroupBy(name => name).Select(group => group.Key + " x" + group.Count());
    loc.m_prefab.Release();
    return string.Join(", ", items);
  }
  private GameObject? goCollider;
  public DebugCommands()
  {
    new Terminal.ConsoleCommand("ew_map", "Refreshes the world map.", (args) =>
    {
      WorldInfo.RegenerateMap();
    }, true);
    new Terminal.ConsoleCommand("ew_biomes", "[precision] - Counts biomes by sampling points with a given precision (meters).", args =>
    {
      var precision = 100f;
      if (args.Length > 1 && int.TryParse(args[1], out var value)) precision = (float)value;
      var r = WorldInfo.Radius;
      var start = -(float)Math.Ceiling(r / precision) * precision;
      Dictionary<Heightmap.Biome, int> biomes = [];
      for (var x = start; x <= r; x += precision)
      {
        for (var y = start; y <= r; y += precision)
        {
          var distance = new Vector2(x, y).magnitude;
          if (distance > r) continue;
          var biome = WorldGenerator.instance.GetBiome(x, y);
          if (!biomes.ContainsKey(biome)) biomes[biome] = 0;
          biomes[biome]++;
        }
      }
      float total = biomes.Values.Sum();
      var text = biomes
        .OrderBy(kvp => Enum.GetName(typeof(Heightmap.Biome), kvp.Key))
        .Select(kvp => $"{Enum.GetName(typeof(Heightmap.Biome), kvp.Key)} ({(int)kvp.Key}): {kvp.Value}/{total} ({(kvp.Value / total).ToString("P2", CultureInfo.InvariantCulture)}");
      ZLog.Log(string.Join("\n", text));
      args.Context.AddString(string.Join("\n", text));
    }, true);
    new Terminal.ConsoleCommand("ew_musics", "- Prints available musics.", args =>
    {
      var mm = MusicMan.instance;
      if (!mm) return;
      var names = mm.m_music.Where(music => music.m_enabled).Select(music => music.m_name);
      ZLog.Log(string.Join("\n", names));
      args.Context.AddString(string.Join("\n", names));
    }, true);
    new Terminal.ConsoleCommand("ew_icons", "- Prints available location icons.", args =>
    {
      var mm = Minimap.instance;
      if (!mm) return;
      var names = mm.m_locationIcons.Select(icon => icon.m_name).Concat(mm.m_icons.Select(icon => icon.m_name.ToString()));
      var db = ObjectDB.instance;
      if (db) names = names.Concat(db.m_items.Select(item => item.name));
      if (db) names = names.Concat(db.m_StatusEffects.Select(effect => effect.m_name));
      ZLog.Log(string.Join("\n", names));
      args.Context.AddString(string.Join("\n", names));
    }, true);

    new Terminal.ConsoleCommand("ew_rooms", "- Logs available rooms.", args =>
    {
      var db = DungeonDB.instance;
      if (!db) return;
      var names = db.m_rooms.Select(room => $"{room.m_prefab.Name} ({DataManager.FromEnum(room.m_theme)}): {GetRoomItems(room)}").ToList();
      ZLog.Log(string.Join("\n", names));
      args.Context.AddString($"Logged {names.Count} rooms to the log file.");
    }, true);
    new Terminal.ConsoleCommand("ew_locations", "- Logs available locations.", args =>
    {
      var zs = ZoneSystem.instance;
      if (!zs) return;
      var names = zs.m_locations.Where(loc => loc.m_prefab.IsValid && loc.m_prefab.Name != "Blueprint").Select(loc => $"{loc.m_prefab.Name}: {GetLocationItems(loc)}").ToList();
      ZLog.Log(string.Join("\n", names));
      args.Context.AddString($"Logged {names.Count} locations to the log file.");
    }, true);
    new Terminal.ConsoleCommand("ew_dungeons", "- Logs dungeons and their available rooms.", args =>
    {
      var zs = ZoneSystem.instance;
      if (!zs) return;
      GameObject obj = new();
      var dg = obj.AddComponent<DungeonGenerator>();
      var dgs = Dungeon.DungeonObjects.Generators.Select(kvp =>
      {
        Dungeon.Spawner.Override(dg, kvp.Key);
        dg.SetupAvailableRooms();
        var rooms = DungeonGenerator.m_availableRooms.Select(room => room.m_prefab.Name);
        return $"{kvp.Key}: {string.Join(", ", rooms)}";
      }).ToList();
      ZLog.Log(string.Join("\n", dgs));
      args.Context.AddString($"Logged {dgs.Count} dungeons to the log file.");
    }, true);
    new Terminal.ConsoleCommand("ew_copy_room", "[object] - Saves objects of current room.", args =>
    {
      var name = "";
      if (args.Length < 2)
      {
        name = GetHovered(Player.m_localPlayer);
      }
      else
        name = args.Args[1];
      if (name == "")
      {
        args.Context.AddString("No object specified.");
        return;
      }
      // Find all room objects and find the one where Player is inside.
      var rooms = UnityEngine.Object.FindObjectsOfType<Room>();
      var player = Player.m_localPlayer;
      if (!player) return;
      if (goCollider == null)
      {
        goCollider = UnityEngine.Object.Instantiate(new GameObject("Collider"));
        goCollider.AddComponent<BoxCollider>();
      }
      var collider = goCollider.GetComponent<BoxCollider>();
      var room = rooms.Where(room =>
      {
        collider.size = new Vector3(room.m_size.x, room.m_size.y, room.m_size.z);
        var tr = room.transform;
        collider.transform.position = tr.position;
        collider.transform.rotation = tr.rotation;
        return Contains(collider, player.transform.position);
      }).OrderBy(room => Utils.DistanceXZ(room.transform.position, player.transform.position)).FirstOrDefault();
      if (!room)
      {
        args.Context.AddString("No room found.");
        return;
      }
      var db = DungeonDB.instance;
      if (!db) return;
      var zone = ZoneSystem.instance.GetZone(room.transform.position);

      collider.size = new Vector3(room.m_size.x, room.m_size.y, room.m_size.z);
      collider.transform.position = room.transform.position;
      collider.transform.rotation = room.transform.rotation;

      ZNetScene.instance.m_tempCurrentObjects.Clear();
      ZDOMan.instance.FindSectorObjects(zone, 1, 0, ZNetScene.instance.m_tempCurrentObjects);
      var prefab = name.GetStableHashCode();
      var inside = ZNetScene.instance.m_tempCurrentObjects
        .Where(zdo => zdo.GetPrefab() == prefab)
        .Where(zdo => Contains(collider, zdo.GetPosition()))
        .OrderBy(zdo => Utils.DistanceXZ(zdo.GetPosition(), room.transform.position))
        .ToList();
      var roomRot = Quaternion.Inverse(room.transform.rotation);
      var lines = string.Join("\n", inside.Select(zdo =>
      {
        var pos = roomRot * (zdo.GetPosition() - room.transform.position);
        var rot = roomRot * zdo.GetRotation();
        return $"  - {name}, {Helper.Print(pos)}, {Helper.Print(rot)}";
      }));
      GUIUtility.systemCopyBuffer = lines;
      args.Context.AddString($"{inside.Count} objects copied for room {room.name}");
    }, true, optionsFetcher: () => ZNetScene.instance.m_namedPrefabs.Select(prefab => prefab.Value.name).ToList());

    new Terminal.ConsoleCommand("ew_copy_location", "[object] [distance=50] - Saves objects of current location.", args =>
    {
      var name = "";
      if (args.Length < 2)
      {
        name = GetHovered(Player.m_localPlayer);
      }
      else
        name = args.Args[1];
      if (name == "")
      {
        args.Context.AddString("No object specified.");
        return;
      }
      var distance = args.TryParameterFloat(2, 50f);
      var locations = UnityEngine.Object.FindObjectsOfType<LocationProxy>();
      var player = Player.m_localPlayer;
      if (!player) return;
      var loc = locations
        .Where(loc => Utils.DistanceXZ(loc.transform.position, player.transform.position) < distance)
        .OrderBy(loc => Utils.DistanceXZ(loc.transform.position, player.transform.position))
        .FirstOrDefault();
      if (!loc)
      {
        args.Context.AddString("No location found.");
        return;
      }
      var zone = ZoneSystem.instance.GetZone(loc.transform.position);

      ZNetScene.instance.m_tempCurrentObjects.Clear();
      ZDOMan.instance.FindSectorObjects(zone, 1, 0, ZNetScene.instance.m_tempCurrentObjects);
      var prefab = name.GetStableHashCode();
      var inside = ZNetScene.instance.m_tempCurrentObjects
        .Where(zdo => zdo.GetPrefab() == prefab)
        .Where(zdo => Utils.DistanceXZ(zdo.GetPosition(), loc.transform.position) < distance)
        .OrderBy(zdo => Utils.DistanceXZ(zdo.GetPosition(), loc.transform.position))
        .ToList();
      var locRot = Quaternion.Inverse(loc.transform.rotation);
      var lines = string.Join("\n", inside.Select(zdo =>
      {
        var pos = locRot * (zdo.GetPosition() - loc.transform.position);
        var rot = locRot * zdo.GetRotation();
        return $"  - {name}, {Helper.Print(pos)}, {Helper.Print(rot)}";
      }));
      GUIUtility.systemCopyBuffer = lines;
      args.Context.AddString($"{inside.Count} objects copied for room {loc.m_instance.name}");
    }, true, optionsFetcher: () => ZNetScene.instance.m_namedPrefabs.Select(prefab => prefab.Value.name).ToList());
  }
  private static bool Contains(Collider collider, Vector3 worldPosition)
  {
    var direction = collider.bounds.center - worldPosition;
    var ray = new Ray(worldPosition, direction);

    return collider.Raycast(ray, out var _, direction.magnitude);
  }

  private static string GetHovered(Player obj)
  {
    var raycast = 50f;
    var mask = LayerMask.GetMask(
    [
      "item",
      "piece",
      "piece_nonsolid",
      "Default",
      "static_solid",
      "Default_small",
      "character",
      "character_net",
      "terrain",
      "vehicle",
      "character_trigger" // Added to remove spawners with ESP mod.
    ]);
    var hits = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, raycast, mask);
    Array.Sort(hits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    foreach (var hit in hits)
    {
      if (Vector3.Distance(hit.point, obj.m_eye.position) >= 50f) continue;
      var netView = hit.collider.GetComponentInParent<ZNetView>();
      if (!netView) continue;
      if (hit.collider.GetComponent<EffectArea>()) continue;
      var player = netView.GetComponentInChildren<Player>();
      if (player == obj) continue;
      return ZNetScene.instance.m_namedPrefabs[netView.GetZDO().GetPrefab()].name;
    }
    return "";
  }
}

