using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Data;

// Either a data entry name (or shorthand/base64 string) or an inline data definition.
public sealed class DataField(string? name, DataYaml? inline = null)
{
  public string? Name = name;
  public DataYaml? Inline = inline;
}

internal sealed class DataFieldConverter : IYamlTypeConverter
{
  public bool Accepts(Type type) => type == typeof(DataField);

  private static readonly IDeserializer BlockDeserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

  public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
  {
    if (parser.Current is MappingStart)
      return new DataField(null, (DataYaml?)rootDeserializer(typeof(DataYaml)));
    if (parser.Current is Scalar scalar)
    {
      parser.MoveNext();
      if (scalar.Style == ScalarStyle.Literal || scalar.Style == ScalarStyle.Folded)
        return new DataField(null, BlockDeserializer.Deserialize<DataYaml>(scalar.Value));
      return string.IsNullOrWhiteSpace(scalar.Value) ? null : new DataField(scalar.Value);
    }
    rootDeserializer(typeof(object));
    return null;
  }

  public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
  {
    if (value is not DataField field) return;
    if (field.Inline != null) serializer(field.Inline, typeof(DataYaml));
    else emitter.Emit(new Scalar(field.Name ?? ""));
  }
}
