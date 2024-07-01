namespace Data;

public class BoolValue(string[] values) : AnyValue(values), IBoolValue
{
  public int? GetInt(Pars pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true" ? 1 : 0;
  }
  public bool? GetBool(Pars pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return value == "true";
  }
  public bool? Match(Pars pars, bool value)
  {

    // If all values are null, default to a match.
    var allNull = true;
    foreach (var rawValue in Values)
    {
      var v = ReplaceParameters(rawValue, pars);
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

  public int? GetInt(Pars pars) => Value ? 1 : 0;
  public bool? GetBool(Pars pars) => Value;
  public bool? Match(Pars pars, bool value) => Value == value;
}

public interface IBoolValue
{
  int? GetInt(Pars pars);
  bool? GetBool(Pars pars);
  bool? Match(Pars pars, bool value);
}
