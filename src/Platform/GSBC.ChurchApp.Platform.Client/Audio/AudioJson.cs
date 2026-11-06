using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Audio;

internal sealed record AudioItemJson(string Id, string Url, string Title, string Artist, string Album, string? ArtworkUrl, double Duration, string? Link);

internal sealed record AudioStateJson(
    AudioItemJson? Item, bool Playing, bool Buffering, double Position, double Duration, double Rate,
    bool Ended, string? Problem);

internal sealed record PlaybackRecordJson(string Id, double Position, double Duration, long HeardAt, Dictionary<string, long>? Marks, long Seq);

internal sealed record AudioDownloadJson(string Id, string Title, long Bytes, long Expected, string State);

internal sealed record PlaybackJournalJson(PlaybackRecordJson[] Records, long LatestSeq);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AudioItemJson))]
[JsonSerializable(typeof(AudioItemJson[]))]
[JsonSerializable(typeof(AudioStateJson))]
[JsonSerializable(typeof(PlaybackJournalJson))]
[JsonSerializable(typeof(AudioDownloadJson[]))]
internal sealed partial class AudioJson : JsonSerializerContext;
