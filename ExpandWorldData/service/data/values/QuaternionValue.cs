using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;

namespace Data;

public class QuaternionValue(string[] values) : AnyValue(values), IQuaternionValue
{
  public Quaternion? Get(Dictionary<string, string> pars) => Parse.AngleYXZNull(GetValue(pars));
}

public class SimpleQuaternionValue(Quaternion value) : IQuaternionValue
{
  private readonly Quaternion Value = value;
  public Quaternion? Get(Dictionary<string, string> pars) => Value;
}

public interface IQuaternionValue
{
  Quaternion? Get(Dictionary<string, string> pars);
}
