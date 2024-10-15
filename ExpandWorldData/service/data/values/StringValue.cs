using System.Linq;

namespace Data;

public class StringValue(string[] values) : AnyValue(values), IStringValue
{
  private readonly bool IsPattern = values.Any(v => v.Contains("*"));
  public string? Get(Parameters pars) => GetValue(pars);

  public bool? Match(Parameters pars, string value)
  {
    var values = GetAllValues(pars);
    if (values.Count == 0) return null;
    return IsPattern ? values.Any(v => SimpleStringValue.PatternMatch(value, v)) : values.Contains(value);
  }
}
public class SimpleStringValue(string value) : IStringValue
{
  private readonly string Value = value;
  private readonly bool IsPattern = value.Contains("*");
  public string? Get(Parameters pars) => Value;
  public bool? Match(Parameters pars, string value) => IsPattern ? PatternMatch(value, Value) : Value == value;
  public static bool PatternMatch(string value, string pattern)
  {
    if (value == pattern) return true;
    if (pattern == "") return false;
    if (pattern[0] == '*' && pattern[pattern.Length - 1] == '*')
    {
      return value.Contains(pattern.Substring(1, pattern.Length - 2));
    }
    if (pattern[0] == '*')
    {
      return value.EndsWith(pattern.Substring(1), System.StringComparison.Ordinal);
    }
    if (pattern[pattern.Length - 1] == '*')
    {
      return value.StartsWith(pattern.Substring(0, pattern.Length - 1), System.StringComparison.Ordinal);
    }
    return false;
  }
}
public interface IStringValue
{
  string? Get(Parameters pars);
  bool? Match(Parameters pars, string value);
}