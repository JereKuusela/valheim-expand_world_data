using System.Linq;
using Service;

namespace Data;

public class ZdoIdValue(string[] values) : AnyValue(values), IZdoIdValue
{
  public ZDOID? Get(Pars pars)
  {
    var value = GetValue(pars);
    if (value == null) return null;
    return Parse.ZdoId(value);
  }
  public bool? Match(Pars pars, ZDOID value)
  {
    var values = GetAllValues(pars);
    if (values.Length == 0) return null;
    return values.Any(v => Parse.ZdoId(v) == value);
  }
}

public class SimpleZdoIdValue(ZDOID value) : IZdoIdValue
{
  private readonly ZDOID Value = value;
  public ZDOID? Get(Pars pars) => Value;
  public bool? Match(Pars pars, ZDOID value) => Value == value;
}

public interface IZdoIdValue
{
  ZDOID? Get(Pars pars);
  bool? Match(Pars pars, ZDOID value);
}
