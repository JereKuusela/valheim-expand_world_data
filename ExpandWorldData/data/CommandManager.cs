
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ExpandWorldData;

public class CommandManager
{

  public static void Run(IEnumerable<string> commands, Vector3 center, Vector3 rot)
  {
    foreach (var command in commands)
    {
      var cmd = Parse(command, center, rot);
      Console.instance.TryRunCommand(cmd);
    }
  }
  private static string Parse(string command, Vector3 center, Vector3 rot)
  {
    var cmd = command
        .Replace("$$x", center.x.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("$$y", center.y.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("$$z", center.z.ToString(NumberFormatInfo.InvariantInfo))
        .Replace("$$a", rot.y.ToString(NumberFormatInfo.InvariantInfo));

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
    if (minus.Length > 1)
    {
      var sum = Evaluate(minus[0]);
      for (var i = 1; i < minus.Length; ++i) sum -= Evaluate(minus[i]);
      return sum;
    }
    return float.Parse(expression.Trim(), NumberFormatInfo.InvariantInfo);
  }
}
