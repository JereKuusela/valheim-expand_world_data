namespace Data;

public class BoolValue(string[] values) : AnyValue(values), IBoolValue
{
  public int? GetInt(Parameters pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true" ? 1 : 0;
  }
  public bool? GetBool(Parameters pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true";
  }
  public bool? Match(Parameters pars, bool value)
  {

    // If all values are null, default to a match.
    var allNull = true;
    foreach (var rawValue in Values)
    {
      var v = pars.Replace(rawValue);
      if (v == null) continue;
      allNull = false;
      var truthy = v == "true";
      if (truthy == value)
        return true;
    }
    return allNull ? null : false;
  }
}

public class SimpleBoolValue(bool value) : IBoolValue
{
  private readonly bool Value = value;

  public int? GetInt(Parameters pars) => Value ? 1 : 0;
  public bool? GetBool(Parameters pars) => Value;
  public bool? Match(Parameters pars, bool value) => Value == value;
}

public interface IBoolValue
{
  int? GetInt(Parameters pars);
  bool? GetBool(Parameters pars);
  bool? Match(Parameters pars, bool value);
}
