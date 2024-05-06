
using System.Collections.Generic;

namespace Data;

public class StringValue(string[] values) : AnyValue(values), IStringValue
{
  public string? Get(Dictionary<string, string> pars) => GetValue(pars);
}
public class SimpleStringValue(string value) : IStringValue
{
  private readonly string Value = value;
  public string? Get(Dictionary<string, string> pars) => Value;
}
public interface IStringValue
{
  string? Get(Dictionary<string, string> pars);
}