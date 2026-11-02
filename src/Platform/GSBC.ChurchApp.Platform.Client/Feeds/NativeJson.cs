using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

internal sealed record StoredRecordJson(string Id, long Version, string Payload, string? SortKey, long? StartsAt, long? EndsAt, string? GroupKey);

internal sealed record StoredFeedJson(string Feed, int Rows, long Bytes, long Cursor);

internal sealed record StoreInfoJson(long FileBytes, StoredFeedJson[] Feeds);

internal sealed record OutboxEntryJson(string Id, string Method, long CreatedAt, int Attempts, string? LastError, bool Failed);

internal sealed record PrefetchItemJson(string Key, string Url, long Bytes, bool IsPrivate);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredRecordJson[]))]
[JsonSerializable(typeof(StoreInfoJson))]
[JsonSerializable(typeof(OutboxEntryJson[]))]
[JsonSerializable(typeof(PrefetchItemJson[]))]
internal sealed partial class NativeJson : JsonSerializerContext;
