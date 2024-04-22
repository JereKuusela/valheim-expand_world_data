using System.Collections.Generic;
using System.Linq;
using Service;
using UnityEngine;

namespace Data;

public class Vector3Value(string[] values) : AnyValue(values), IVector3Value
{
  public Vector3? Get(Dictionary<string, string> pars) => Parse.VectorXZYNull(GetValue(pars));
}

public class SimpleVector3Value(Vector3 value) : IVector3Value
{
  private readonly Vector3 Value = value;
  public Vector3? Get(Dictionary<string, string> pars) => Value;
}

public interface IVector3Value
{
  Vector3? Get(Dictionary<string, string> pars);
}