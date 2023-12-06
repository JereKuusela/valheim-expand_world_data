
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
  public long Character;
  public string HostId;
  public ZDOID ZDOID;
  public PlayerInfo(ZNetPeer peer)
  {
    Name = peer.m_playerName;
    Pos = peer.m_refPos;
    Character = peer.m_uid;
    ZDOID = peer.m_characterID;
    HostId = peer.m_rpc.GetSocket().GetHostName();

  }
  public PlayerInfo(Player player)
  {
    Name = player.GetPlayerName();
    Pos = player.transform.position;
    Character = ZNet.GetUID();
    ZDOID = player.GetZDOID();
    HostId = "server";
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
  private static string Parse(string command, Vector3 center, Vector3 rot, PlayerInfo? peer)
  {
    var cmd = command
        .Replace("<x>", center.x.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<y>", center.y.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<z>", center.z.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<a>", rot.y.ToString(NumberFormatInfo.InvariantInfo));
    if (peer != null)
    {
      cmd = cmd
        .Replace("<px>", peer.Pos.x.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<py>", peer.Pos.y.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<pz>", peer.Pos.z.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<pname>", peer.Name)
        .Replace("<pid>", peer.HostId.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("<pchar>", peer.Character.ToString(NumberFormatInfo.InvariantInfo));
    }
    var expressions = cmd.Split(' ').Select(s => s.Split('=')).Select(a => a[a.Length - 1].Trim()).SelectMany(s => s.Split(',')).ToArray();
    foreach (var expression in expressions)
    {
      if (!expression.Contains('*') && !expression.Contains('/') && !expression.Contains('+') && !expression.Contains('-')) continue;
      var value = Evaluate(expression);
      cmd = cmd.Replace(expression, value.ToString(NumberFormatInfo.InvariantInfo));
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
