using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Files;

internal sealed record KeptFileJson(string Id, string? Url, string? Title, long Bytes, long Expected, string? State);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(KeptFileJson[]))]
internal sealed partial class KeptFilesJson : JsonSerializerContext;
