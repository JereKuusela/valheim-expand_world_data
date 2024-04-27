
using System.Collections.Generic;
using UnityEngine;

namespace Data;

public class LongValue(string[] values) : AnyValue(values), ILongValue
{
  public long? Get(Dictionary<string, string> pars)
  {
    var value = GetValue(pars);
    if (value == null)
      return null;
    if (!value.Contains(";"))
      return Calculator.EvaluateLong(value);
    // Format for range is "start;end;step;statement".
    var split = value.Split(';');
    if (split.Length < 2)
      throw new System.InvalidOperationException($"Invalid range format: {value}");
    var min = Calculator.EvaluateLong(split[0]);
    var max = Calculator.EvaluateLong(split[1]);
    if (min == null || max == null)
      return null;
    long? roll;
    if (split.Length < 3 || split[2] == "")
      roll = (long?)(Random.value * (max.Value - min.Value) + min.Value);
    else
    {
      var step = Calculator.EvaluateLong(split[2]);
      if (step == null)
        roll = (long?)(Random.value * (max.Value - min.Value) + min.Value);
      else
      {
        var steps = (max - min) / step;
        var rollStep = Random.Range(0, steps.Value + 1);
        roll = (long?)(min + rollStep * step);
      }
    }
    if (split.Length < 4)
      return roll;
    return Calculator.EvaluateLong(split[3].Replace("<value>", roll.ToString()));
  }
}

public class SimpleLongValue(long value) : ILongValue
{
  private readonly long Value = value;
  public long? Get(Dictionary<string, string> pars) => Value;
}

public interface ILongValue
{
  long? Get(Dictionary<string, string> pars);
}
