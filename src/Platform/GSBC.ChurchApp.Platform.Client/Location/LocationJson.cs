using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Location;

internal sealed record LocationFixJson(long Seq, long At, double Lat, double Lon, double Accuracy, double Heading, double Speed);

internal sealed record LocationFixesJson(LocationFixJson[]? Fixes, long Latest, bool Running);

internal sealed record LocationStateJson(bool Running, string? Authorization);

internal sealed record WalkTapJson(long Seq, WalkTapKind Kind, long At);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LocationFixesJson))]
[JsonSerializable(typeof(LocationStateJson))]
[JsonSerializable(typeof(WalkActivityState))]
[JsonSerializable(typeof(WalkTapJson[]))]
internal sealed partial class LocationJson : JsonSerializerContext;
