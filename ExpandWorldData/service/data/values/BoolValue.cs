
using System.Collections.Generic;

namespace Data;

public class BoolValue(string[] values) : AnyValue(values), IBoolValue
{
  public int? GetInt(Dictionary<string, string> pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true" ? 1 : 0;
  }
  public bool? GetBool(Dictionary<string, string> pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true";
  }
}

public class SimpleBoolValue(bool value) : IBoolValue
{
  private readonly bool Value = value;

  public int? GetInt(Dictionary<string, string> pars) => Value ? 1 : 0;
  public bool? GetBool(Dictionary<string, string> pars) => Value;
}

public interface IBoolValue
{
  int? GetInt(Dictionary<string, string> pars);
  bool? GetBool(Dictionary<string, string> pars);
}
