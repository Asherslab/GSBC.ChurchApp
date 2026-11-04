using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Platform.Client.Audio;

internal sealed record AudioItemJson(string Id, string Url, string Title, string Artist, string Album, string? ArtworkUrl, double Duration, string? Link);

internal sealed record AudioStateJson(
    AudioItemJson? Item, bool Playing, bool Buffering, double Position, double Duration, double Rate,
    long? SleepAt, bool SleepAtEnd, bool Ended, string? Problem);

internal sealed record PlaybackRecordJson(string Id, double Position, double Duration, long HeardAt, Dictionary<string, long>? Marks, long Seq);

internal sealed record PlaybackJournalJson(PlaybackRecordJson[] Records, long LatestSeq);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AudioItemJson))]
[JsonSerializable(typeof(AudioItemJson[]))]
[JsonSerializable(typeof(AudioStateJson))]
[JsonSerializable(typeof(PlaybackJournalJson))]
internal sealed partial class AudioJson : JsonSerializerContext;
