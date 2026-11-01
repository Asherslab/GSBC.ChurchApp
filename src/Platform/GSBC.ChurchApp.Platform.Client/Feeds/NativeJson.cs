using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

internal sealed record StoredRecordJson(string Id, long Version, string Payload, string? SortKey, long? StartsAt, long? EndsAt, string? GroupKey);

internal sealed record StoredFeedJson(string Feed, int Rows, long Bytes, long Cursor);

internal sealed record StoreInfoJson(long FileBytes, StoredFeedJson[] Feeds);

internal sealed record OutboxEntryJson(string Id, string Method, long CreatedAt, int Attempts, string? LastError, bool Failed);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredRecordJson[]))]
[JsonSerializable(typeof(StoreInfoJson))]
[JsonSerializable(typeof(OutboxEntryJson[]))]
internal sealed partial class NativeJson : JsonSerializerContext;
