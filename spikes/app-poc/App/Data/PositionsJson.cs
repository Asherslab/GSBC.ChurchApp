using System.Text.Json.Serialization;

namespace Gsbc.AppPoc.Data;

[JsonSerializable(typeof(Dictionary<string, double>))]
public sealed partial class PositionsJson : JsonSerializerContext;
