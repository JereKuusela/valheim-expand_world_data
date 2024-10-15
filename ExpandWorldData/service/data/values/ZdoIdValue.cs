using System.Linq;
using Service;

namespace Data;

public class ZdoIdValue(string[] values) : AnyValue(values), IZdoIdValue
{
  public ZDOID? Get(Parameters pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return Parse.ZdoId(value);
  }
  public bool? Match(Parameters pars, ZDOID value)
  {
    var values = GetAllValues(pars);
    if (values.Count == 0) return null;
    return values.Any(v => Parse.ZdoId(v) == value);
  }
}

public class SimpleZdoIdValue(ZDOID value) : IZdoIdValue
{
  private readonly ZDOID Value = value;
  public ZDOID? Get(Parameters pars) => Value;
  public bool? Match(Parameters pars, ZDOID value) => Value == value;
}

public interface IZdoIdValue
{
  ZDOID? Get(Parameters pars);
  bool? Match(Parameters pars, ZDOID value);
}
