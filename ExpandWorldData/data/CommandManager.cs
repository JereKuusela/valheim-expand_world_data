
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ExpandWorldData;

public class PlayerInfo
{
  public string Name;
  public Vector3 Pos;
  public Quaternion Rot;
  public long Character;
  public string HostId;
  public ZDOID ZDOID;
  public PlayerInfo(ZNetPeer peer)
  {
    HostId = peer.m_rpc.GetSocket().GetHostName();
    Name = peer.m_playerName;
    Pos = peer.m_refPos;
    ZDOID = peer.m_characterID;
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
  }
}

public class CommandManager
{
  public static void Run(IEnumerable<string> commands, Vector3 center, Vector3 rot) => Run(commands, center, rot, (PlayerInfo?)null);
  public static void Run(IEnumerable<string> commands, Vector3 center, Vector3 rot, PlayerInfo? player)
  {
    foreach (var command in commands)
    {
      try
      {
        var cmd = Parse(command, center, rot, player);
        Console.instance.TryRunCommand(cmd);
      }
      catch (Exception e)
      {
        EWD.Log.LogError($"Failed to run command: {command}\n{e.Message}");
      }
    }
  }
  public static void Run(IEnumerable<string> commands, Vector3 center, Vector3 rot, PlayerInfo[] players)
  {
    foreach (var peer in players)
      Run(commands, center, rot, peer);
  }
  private static string Parse(string command, Vector3 center, Vector3 rot, PlayerInfo? player)
  {
    var cmd = command
        .Replace("<x>", center.x.ToString("0.#####"))
        .Replace("<y>", center.y.ToString("0.#####"))
        .Replace("<z>", center.z.ToString("0.#####"))
        .Replace("<a>", rot.y.ToString("0.#####"));
    if (player != null)
    {
      cmd = cmd
        .Replace("<px>", player.Pos.x.ToString("0.#####"))
        .Replace("<py>", player.Pos.y.ToString("0.#####"))
        .Replace("<pz>", player.Pos.z.ToString("0.#####"))
        .Replace("<pname>", player.Name)
        .Replace("<pid>", player.HostId)
        .Replace("<pchar>", player.Character.ToString());
    }
    var expressions = cmd.Split(' ').Select(s => s.Split('=')).Select(a => a[a.Length - 1].Trim()).SelectMany(s => s.Split(',')).ToArray();
    foreach (var expression in expressions)
    {
      // Single negative number would get handled as expression.
      var sub = expression.Substring(1);
      if (!sub.Contains('*') && !sub.Contains('/') && !sub.Contains('+') && !sub.Contains('-')) continue;
      var value = Evaluate(expression);
      cmd = cmd.Replace(expression, value.ToString("0.#####"));
    }
    return cmd;
  }
  private static float Evaluate(string expression)
  {
    var mult = expression.Split('*');
    if (mult.Length > 1)
    {
      var sum = 1f;
      foreach (var m in mult) sum *= Evaluate(m);
      return sum;
    }
    var div = expression.Split('/');
    if (div.Length > 1)
    {
      var sum = Evaluate(div[0]);
      for (var i = 1; i < div.Length; ++i) sum /= Evaluate(div[i]);
      return sum;
    }
    var plus = expression.Split('+');
    if (plus.Length > 1)
    {
      var sum = 0f;
      foreach (var p in plus) sum += Evaluate(p);
      return sum;
    }
    var minus = expression.Split('-');
    // Negative numbers get split as well, so check for actual parts.
    if (minus.Where(s => s != "").Count() > 1)
    {
      float? sum = null;
      for (var i = 0; i < minus.Length; ++i)
      {
        if (minus[i] == "" && i + 1 < minus.Length)
        {
          minus[i + 1] = "-" + minus[i + 1];
          continue;
        }
        if (sum == null) sum = Evaluate(minus[i]);
        else sum -= Evaluate(minus[i]);
      }
      return sum ?? 0;
    }
    try
    {
      return float.Parse(expression.Trim(), NumberFormatInfo.InvariantInfo);
    }
    catch
    {
      throw new Exception($"Failed to parse expression: {expression}");
    }
  }
}
