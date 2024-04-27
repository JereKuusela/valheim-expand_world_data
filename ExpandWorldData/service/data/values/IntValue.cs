
using System.Collections.Generic;
using System.Globalization;
using Service;
using UnityEngine;

namespace Data;

public class IntValue(string[] values) : AnyValue(values), IIntValue
{
  public int? Get(Dictionary<string, string> pars)
  {
    var value = GetValue(pars);
    if (value == null)
      return null;
    if (!value.Contains(";"))
      return Calculator.EvaluateInt(value);
    // Format for range is "start;end;step;statement".
    var split = value.Split(';');
    if (split.Length < 2)
      throw new System.InvalidOperationException($"Invalid range format: {value}");
    var min = Calculator.EvaluateInt(split[0]);
    var max = Calculator.EvaluateInt(split[1]);
    if (min == null || max == null)
      return null;
    int? roll;
    if (split.Length < 3 || split[2] == "")
      roll = Random.Range(min.Value, max.Value + 1);
    else
    {
      var step = Calculator.EvaluateInt(split[2]);
      if (step == null)
        roll = Random.Range(min.Value, max.Value + 1);
      else
      {
        var steps = (max - min) / step;
        var rollStep = Random.Range(0, steps.Value + 1);
        roll = min + rollStep * step;
      }
    }
    if (split.Length < 4)
      return roll;
    return Calculator.EvaluateInt(split[3].Replace("<value>", roll.ToString()));
  }
}

public class SimpleIntValue(int value) : IIntValue
{
  private readonly int Value = value;
  public int? Get(Dictionary<string, string> pars) => Value;
}
public interface IIntValue
{
  int? Get(Dictionary<string, string> pars);
}