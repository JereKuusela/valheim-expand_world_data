
using System.Collections.Generic;
using System.Globalization;
using ExpandWorldData;
using UnityEngine;

namespace Data;

public class FloatValue(string[] values) : AnyValue(values), IFloatValue
{
  public float? Get(Dictionary<string, string> pars)
  {
    var value = GetValue(pars);
    if (value == null)
      return null;
    if (!value.Contains(";"))
      return Calculator.EvaluateFloat(value);
    // Format for range is "start;end;step;statement".
    var split = value.Split(';');
    if (split.Length < 2)
      throw new System.InvalidOperationException($"Invalid range format: {value}");
    var min = Calculator.EvaluateFloat(split[0]);
    var max = Calculator.EvaluateFloat(split[1]);
    if (min == null || max == null)
      return null;
    float? roll;
    if (split.Length < 3 || split[2] == "")
      roll = Random.Range(min.Value, max.Value);
    else
    {
      var step = Calculator.EvaluateFloat(split[2]);
      if (step == null)
        roll = Random.Range(min.Value, max.Value);
      else
      {
        var steps = (int)((max.Value - min.Value) / step.Value);
        var rollStep = Random.Range(0, steps + 1);
        roll = min + rollStep * step;
      }
    }
    if (split.Length < 4)
      return roll;
    return Calculator.EvaluateFloat(split[3].Replace("<value>", roll?.ToString(CultureInfo.InvariantCulture)));
  }
}

public class SimpleFloatValue(float value) : IFloatValue
{
  private readonly float Value = value;
  public float? Get(Dictionary<string, string> pars) => Value;
}
public interface IFloatValue
{
  float? Get(Dictionary<string, string> pars);
}