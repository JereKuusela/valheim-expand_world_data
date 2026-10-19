
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Data;
using UnityEngine;

namespace Service;

public class PlayerInfo
{
  public string Name;
  public Vector3 Pos;
  public Quaternion Rot;
  public long Character;
  public string HostId;
  public ZDOID ZDOID;
  public long PeerId;
  public PlayerInfo(ZNetPeer peer)
  {
    HostId = peer.m_rpc.GetSocket().GetHostName();
    Name = peer.m_playerName;
    Pos = peer.m_refPos;
    ZDOID = peer.m_characterID;
    PeerId = peer.m_uid;
    var zdo = ZDOMan.instance.GetZDO(peer.m_characterID);
    if (zdo != null)
    {
      Character = zdo.GetLong(ZDOVars.s_playerID, 0L);
      Pos = zdo.m_position;
      Rot = zdo.GetRotation();
    }

  }
  public PlayerInfo(Player player)
  {
    HostId = "self";
    Name = player.GetPlayerName();
    ZDOID = player.GetZDOID();
    Character = player.GetPlayerID();
    Pos = player.transform.position;
    Rot = player.transform.rotation;
    PeerId = ZRoutedRpc.instance.m_id;
  }
}

public class CommandManager
{
  public static void RunCommand(IEnumerable<string> commands) => RunCommand(commands, (PlayerInfo?)null);
  public static void RunCommand(IEnumerable<string> commands, Vector3 center, Vector3 rot) => RunCommand(commands, center, rot, null);
  public static void RunCommand(IEnumerable<string> commands, PlayerInfo? player) => RunCommand(commands, null, null, player);
  private static void RunCommand(IEnumerable<string> commands, Vector3? center, Vector3? rot, PlayerInfo? player)
  {
    var parsed = commands.Select(c => Parse(c, center, rot, player)).ToArray();
    foreach (var cmd in parsed)
    {
      try
      {
        Console.instance.TryRunCommand(cmd);
      }
      catch (Exception e)
      {
        Log.Error($"Failed to run command: {cmd}\n{e.Message}");
      }
    }
  }
  public static void RunCommand(IEnumerable<string> commands, List<PlayerInfo> players)
  {
    foreach (var peer in players)
      RunCommand(commands, peer);
  }
  private static string Parse(string command, Vector3? center, Vector3? rot, PlayerInfo? player)
  {
    var cmd = command;
    if (center.HasValue)
    {
      var c = center.Value;
      var zone = ZoneSystem.GetZone(c);
      cmd = cmd
        .Replace("<x>", c.x.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<y>", c.y.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<z>", c.z.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<i>", zone.x.ToString())
        .Replace("<j>", zone.y.ToString());
    }
    if (rot.HasValue)
      cmd = cmd.Replace("<a>", rot.Value.y.ToString("0.#####", NumberFormatInfo.InvariantInfo));
    if (player != null)
    {
      cmd = cmd
        .Replace("<px>", player.Pos.x.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<py>", player.Pos.y.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<pz>", player.Pos.z.ToString("0.#####", NumberFormatInfo.InvariantInfo))
        .Replace("<pname>", player.Name)
        .Replace("<pid>", player.HostId)
        .Replace("<pchar>", player.Character.ToString());
    }
    var expressions = cmd.Split(' ').Select(s => s.Split('=')).Select(a => a[a.Length - 1].Trim()).SelectMany(s => s.Split(',')).ToArray();
    foreach (var expression in expressions)
    {
      if (expression.Length == 0) continue;
      // Single negative number would get handled as expression.
      var sub = expression.Substring(1);
      if (!sub.Contains('*') && !sub.Contains('/') && !sub.Contains('+') && !sub.Contains('-')) continue;
      var value = Calculator.EvaluateFloat(expression) ?? 0f;
      cmd = cmd.Replace(expression, value.ToString("0.#####", NumberFormatInfo.InvariantInfo));
    }
    return cmd;
  }
}
