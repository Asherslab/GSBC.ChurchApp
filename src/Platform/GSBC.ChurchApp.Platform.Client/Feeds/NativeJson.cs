using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

internal sealed record StoredRecordJson(string Id, long Version, string Payload, string? SortKey, long? StartsAt, long? EndsAt, string? GroupKey);

internal sealed record StoredFeedJson(string Feed, int Rows, long Bytes, long Cursor);

internal sealed record StoreInfoJson(long FileBytes, StoredFeedJson[] Feeds);

internal sealed record OutboxEntryJson(string Id, string Method, long CreatedAt, int Attempts, string? LastError, bool Failed, bool WaitsForUpload, OutboxUploadJson? Upload);

internal sealed record OutboxUploadJson(string File, string FileName, long Bytes);

internal sealed record StagedFileJson(string Token, string Name, long Bytes, string Sha256, string Preview);

internal sealed record QueuedCallJson(string Method, string Request);

internal sealed record QueuedUploadJson(string File, string FileName, string ContentType, long Bytes, QueuedCallJson Find, QueuedCallJson Begin, QueuedCallJson Complete, string[] Paths);

internal sealed record PrefetchItemJson(string Key, string Url, long Bytes, bool IsPrivate);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StoredRecordJson[]))]
[JsonSerializable(typeof(StoreInfoJson))]
[JsonSerializable(typeof(OutboxEntryJson[]))]
[JsonSerializable(typeof(PrefetchItemJson[]))]
[JsonSerializable(typeof(StagedFileJson))]
[JsonSerializable(typeof(QueuedUploadJson[]))]
internal sealed partial class NativeJson : JsonSerializerContext;
