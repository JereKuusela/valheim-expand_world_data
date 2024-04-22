
using System.Collections.Generic;
using System.Linq;

namespace Data;

public class HashValue(string[] values) : AnyValue(values), IHashValue
{
  public int? Get(Dictionary<string, string> pars) => GetValue(pars)?.GetStableHashCode();
}
public class SimpleHashValue(string value) : IHashValue
{
  private readonly int Value = value.GetStableHashCode();

  public int? Get(Dictionary<string, string> pars) => Value;
}
public interface IHashValue
{
  int? Get(Dictionary<string, string> pars);
}