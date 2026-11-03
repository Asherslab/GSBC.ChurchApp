using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Features.Analytics.Client;

internal sealed record NativeCrashJson(string Type, string[] Frames, long CrashedAt, string AppVersion, string AppBuild);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NativeCrashJson[]))]
internal sealed partial class CrashJson : JsonSerializerContext;
