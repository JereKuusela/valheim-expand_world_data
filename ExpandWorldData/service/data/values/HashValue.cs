using System.Linq;

namespace Data;

public class HashValue(string[] values) : AnyValue(values), IHashValue
{
  public int? Get(Pars pars) => GetValue(pars)?.GetStableHashCode();
  public bool? Match(Pars pars, int value)
  {
    var values = GetAllValues(pars);
    if (values.Length == 0) return null;
    return values.Any(v => v.GetStableHashCode() == value);
  }
}
public class SimpleHashValue(string value) : IHashValue
{
  private readonly int Value = value.GetStableHashCode();

  public int? Get(Pars pars) => Value;
  public bool? Match(Pars pars, int value) => Value == value;
}
public interface IHashValue
{
  int? Get(Pars pars);
  bool? Match(Pars pars, int value);
}