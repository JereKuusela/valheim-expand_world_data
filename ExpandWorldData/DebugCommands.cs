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
    var items = room.m_netViews.Select(netView => Utils.GetPrefabName(netView.gameObject)).GroupBy(name => name).Select(group => group.Key + " x" + group.Count());
    return string.Join(", ", items);
  }

  private string GetLocationItems(ZoneSystem.ZoneLocation loc)
  {
    var items = loc.m_netViews.Select(netView => Utils.GetPrefabName(netView.gameObject)).GroupBy(name => name).Select(group => group.Key + " x" + group.Count());
    return string.Join(", ", items);
  }

  public DebugCommands()
  {
    new Terminal.ConsoleCommand("ew_map", "Refreshes the world map.", (args) =>
    {
      WorldInfo.RegenerateMap();
    }, true);
    new Terminal.ConsoleCommand("ew_copy", "[name] - Copies hovered item data to the data yaml.", (args) =>
    {
      if (args.Length < 2) return;
      var name = args[1];
      var hovered = Selector.GetHovered();
      if (hovered == null)
      {
        args.Context.AddString("No hovered object.");
        return;
      }
      ZDOData data = new(name, hovered);
      DataLoading.Save(data);
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
      var names = db.m_rooms.Select(room => $"{room.m_room.name} ({DataManager.FromEnum(room.m_room.m_theme)}): {GetRoomItems(room)}").ToList();
      ZLog.Log(string.Join("\n", names));
      args.Context.AddString($"Logged {names.Count} rooms to the log file.");
    }, true);
    new Terminal.ConsoleCommand("ew_locations", "- Logs available locations.", args =>
    {
      var zs = ZoneSystem.instance;
      if (!zs) return;
      var names = zs.m_locations.Where(loc => loc.m_location && loc.m_location.name != "Blueprint").Select(loc => $"{loc.m_location.name}: {GetLocationItems(loc)}").ToList();
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
        var rooms = DungeonGenerator.m_availableRooms.Select(room => room.m_room.name);
        return $"{kvp.Key}: {string.Join(", ", rooms)}";
      }).ToList();
      ZLog.Log(string.Join("\n", dgs));
      args.Context.AddString($"Logged {dgs.Count} dungeons to the log file.");
    }, true);
  }
}
