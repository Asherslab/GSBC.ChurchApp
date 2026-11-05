using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonPublisher(SermonsDbContext db, TimeProvider time)
{
    public const string Unknown = "Guest speaker";

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        List<SermonRecord> sermons = await db.Sermons.OrderByDescending(sermon => sermon.PreachedAt).ThenBy(sermon => sermon.Id).ToListAsync(cancellationToken);
        Dictionary<Guid, SeriesRecord> series = await db.Series.ToDictionaryAsync(record => record.Id, cancellationToken);
        Dictionary<Guid, SpeakerRecord> speakers = await db.Speakers.ToDictionaryAsync(record => record.Id, cancellationToken);

        List<SermonRecord> window = sermons.Where(sermon => !sermon.Hidden && sermon.AudioUrl.Length > 0).Take(SermonsFeeds.CatalogueWindow).ToList();
        HashSet<Guid> inWindow = window.Select(sermon => sermon.Id).ToHashSet();
        Dictionary<Guid, List<SermonRecord>> bySeries = window.Where(sermon => sermon.EffectiveSeriesId is { } id && series.ContainsKey(id))
            .GroupBy(sermon => sermon.EffectiveSeriesId!.Value).ToDictionary(group => group.Key, group => group.OrderBy(sermon => sermon.PreachedAt).ToList());
        Dictionary<Guid, List<SermonRecord>> bySpeaker = window.Where(sermon => sermon.EffectiveSpeakerId is { } id && speakers.ContainsKey(id))
            .GroupBy(sermon => sermon.EffectiveSpeakerId!.Value).ToDictionary(group => group.Key, group => group.ToList());

        long version = time.GetUtcNow().UtcDateTime.Ticks;
        foreach (SermonRecord sermon in sermons)
        {
            Sermon? payload = inWindow.Contains(sermon.Id) ? Payload(sermon, series, speakers, bySeries) : null;
            if (Publish(SermonsFeeds.Catalogue, sermon.Id, payload, sermon.PublishedHash, version, out string? hash,
                    row => { row.SortKey = SortKey(sermon); row.StartsAt = sermon.PreachedAt; row.GroupKey = sermon.EffectiveSeriesId?.ToString(); }))
                sermon.PublishedHash = hash;
        }

        foreach (SeriesRecord record in series.Values)
        {
            SeriesInfo? payload = bySeries.TryGetValue(record.Id, out List<SermonRecord>? its) ? SeriesPayload(record, its) : null;
            if (Publish(SermonsFeeds.Series, record.Id, payload, record.PublishedHash, version, out string? hash,
                    row => row.SortKey = payload?.LastAt.ToString("O")))
                record.PublishedHash = hash;
        }

        foreach (SpeakerRecord record in speakers.Values)
        {
            SpeakerInfo? payload = bySpeaker.TryGetValue(record.Id, out List<SermonRecord>? its) ? SpeakerPayload(record, its) : null;
            if (Publish(SermonsFeeds.Speakers, record.Id, payload, record.PublishedHash, version, out string? hash,
                    row => row.SortKey = record.Name))
                record.PublishedHash = hash;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public void MarkAdmin(Guid id) =>
        db.FeedChanges.Add(FeedChangeRecord.Marker(SermonsFeeds.Admin, id.ToString(), time.GetUtcNow().UtcDateTime));

    public static Sermon Payload(SermonRecord sermon, IReadOnlyDictionary<Guid, SeriesRecord> series, IReadOnlyDictionary<Guid, SpeakerRecord> speakers,
        IReadOnlyDictionary<Guid, List<SermonRecord>>? bySeries = null)
    {
        SeriesRecord? its = sermon.EffectiveSeriesId is { } seriesId ? series.GetValueOrDefault(seriesId) : null;
        SpeakerRecord? speaker = sermon.EffectiveSpeakerId is { } speakerId ? speakers.GetValueOrDefault(speakerId) : null;
        int part = TitleParts.PartOf(sermon.EffectiveTitle);
        if (part == 0 && its is not null && bySeries?.GetValueOrDefault(its.Id) is { } inOrder)
            part = inOrder.FindIndex(other => other.Id == sermon.Id) + 1;
        return new Sermon
        {
            Id = sermon.Id.ToString(),
            Title = sermon.EffectiveTitle,
            PreachedAt = sermon.PreachedAt,
            Service = sermon.EffectiveService,
            SpeakerId = speaker?.Id.ToString() ?? "",
            SpeakerName = speaker?.Name ?? (sermon.SourceSpeaker.Length > 0 ? sermon.SourceSpeaker : Unknown),
            SeriesId = its?.Id.ToString(),
            SeriesName = its?.Name,
            Part = its is null ? 0 : part,
            AudioUrl = sermon.AudioUrl,
            AudioType = sermon.AudioType,
            AudioBytes = sermon.AudioBytes,
            DurationSeconds = sermon.DurationSeconds,
            YouTubeId = sermon.EffectiveYouTubeId,
            Artwork = Wide(sermon.Artwork) ? sermon.Artwork : its?.ChosenArtwork ?? its?.Artwork ?? sermon.Artwork,
            Thumbnail = Wide(sermon.Thumbnail) ? sermon.Thumbnail : its?.Thumbnail ?? its?.ChosenArtwork ?? sermon.Thumbnail,
            Summary = sermon.Origin == SermonSources.Uploads ? sermon.Summary : "",
            WebLink = sermon.WebLink,
        };
    }

    private static bool Wide(Platform.Contracts.Media.MediaRef? picture) =>
        picture is { Width: > 0, Height: > 0 } && (double)picture.Width / picture.Height >= 1.6;

    private static SeriesInfo SeriesPayload(SeriesRecord record, List<SermonRecord> sermons) => new()
    {
        Id = record.Id.ToString(),
        Name = record.Name,
        Description = record.Description,
        Artwork = record.ChosenArtwork ?? record.Artwork,
        Thumbnail = record.Thumbnail ?? record.ChosenArtwork,
        Count = sermons.Count,
        FirstAt = sermons[0].PreachedAt,
        LastAt = sermons[^1].PreachedAt,
    };

    private static SpeakerInfo SpeakerPayload(SpeakerRecord record, List<SermonRecord> sermons) => new()
    {
        Id = record.Id.ToString(),
        Name = record.Name,
        Photo = record.ChosenPhoto ?? record.Photo,
        Count = sermons.Count,
        LatestAt = sermons.Max(sermon => sermon.PreachedAt),
    };

    private static string SortKey(SermonRecord sermon) => $"{sermon.PreachedAt:O}|{sermon.AddedAt:O}";

    private bool Publish<T>(string feed, Guid id, T? payload, string? publishedHash, long version, out string? hash, Action<FeedChangeRecord> index)
        where T : class
    {
        if (payload is null)
        {
            hash = null;
            if (publishedHash is null)
                return false;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = feed, RecordId = id.ToString(), Version = version, Deleted = true });
            return true;
        }

        byte[] bytes = SermonsPayloads.Encode(payload);
        hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (hash == publishedHash)
            return false;
        FeedChangeRecord row = new() { Feed = feed, RecordId = id.ToString(), Version = version, Payload = bytes };
        index(row);
        db.FeedChanges.Add(row);
        return true;
    }
}

internal static class SermonsPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        SermonsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return SermonsContractsModel.Instance.Deserialize<T>(stream);
    }
}
