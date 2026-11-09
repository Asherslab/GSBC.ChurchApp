using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonLibraryService(
    SermonsDbContext db, SermonPublisher publisher, IMediaReferences media, SermonUploads uploads, PodcastFeedWriter podcast,
    ServiceNames services, SermonDeletion deletion, TimeProvider time) : ISermonLibraryService
{
    public const int TitleLimit = 160;
    public const int NameLimit = 80;
    public const int PassageLimit = 120;
    private const int DescriptionLimit = 600;
    private const int SummaryLimit = 4000;

    public static class Filters
    {
        public const string All = "";
        public const string Hidden = "hidden";
        public const string NeedsALook = "needs-a-look";
    }

    public static class Reasons
    {
        public const string JustUploaded = "Just uploaded";
        public const string NoSeries = "Part of a series?";
        public const string NewSpeaker = "New speaker";
    }

    public async ValueTask<SermonList> ListAsync(SermonQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        IQueryable<SermonRecord> matching = db.Sermons.AsNoTracking();
        if (request.Text.Trim() is { Length: > 0 } text)
        {
            string pattern = Patterns.Like(text);
            List<Guid> speakerIds = await db.Speakers.Where(speaker => EF.Functions.ILike(speaker.Name, pattern)).Select(speaker => speaker.Id).ToListAsync(cancellationToken);
            List<Guid> seriesIds = await db.Series.Where(series => EF.Functions.ILike(series.Name, pattern)).Select(series => series.Id).ToListAsync(cancellationToken);
            matching = matching.Where(sermon => EF.Functions.ILike(sermon.Title, pattern)
                || EF.Functions.ILike(sermon.Service, pattern)
                || EF.Functions.ILike(sermon.Passage, pattern)
                || speakerIds.Contains(sermon.SpeakerId ?? Guid.Empty)
                || seriesIds.Contains(sermon.SeriesId ?? Guid.Empty));
        }

        Dictionary<string, int> counts = new()
        {
            [Filters.All] = await matching.CountAsync(sermon => !sermon.Hidden, cancellationToken),
            [Filters.Hidden] = await matching.CountAsync(sermon => sermon.Hidden, cancellationToken),
            [Filters.NeedsALook] = await matching.CountAsync(sermon => !sermon.Hidden && sermon.CheckedAt == null, cancellationToken),
        };
        IQueryable<SermonRecord> filtered = request.Filter switch
        {
            Filters.Hidden => matching.Where(sermon => sermon.Hidden),
            Filters.NeedsALook => matching.Where(sermon => !sermon.Hidden && sermon.CheckedAt == null),
            _ => matching.Where(sermon => !sermon.Hidden),
        };

        List<SermonRecord> page = await filtered.OrderByDescending(sermon => sermon.PreachedAt).ThenByDescending(sermon => sermon.AddedAt)
            .Skip(Math.Max(0, request.Skip)).Take(request.Take is > 0 and <= 200 ? request.Take : 50).ToListAsync(cancellationToken);
        Lookups lookups = await LookupsAsync(cancellationToken);
        lookups = lookups with { SpeakerCounts = await SpeakerCountsAsync(cancellationToken) };
        return new SermonList
        {
            Sermons = page.Select(sermon => Detail(sermon, lookups)).ToList(),
            Total = counts[request.Filter is Filters.Hidden or Filters.NeedsALook ? request.Filter : Filters.All],
            Counts = counts,
            PodcastFeedUrl = podcast.Url,
        };
    }

    public async ValueTask<SermonDetail> CorrectAsync(SermonCorrection request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        SermonRecord sermon = await FindSermonAsync(request.Id, cancellationToken);
        bool speakerExists = Guid.TryParse(request.SpeakerId, out Guid speakerId) && await db.Speakers.AnyAsync(speaker => speaker.Id == speakerId, cancellationToken);
        bool seriesExists = Guid.TryParse(request.SeriesId, out Guid seriesId) && await db.Series.AnyAsync(series => series.Id == seriesId, cancellationToken);
        if (request.Service is { } service)
            request.Service = await services.NameAsync(service, cancellationToken);
        Correct(sermon, request, speakerExists, seriesExists);

        if (request.Checked is { } isChecked)
        {
            sermon.CheckedAt = isChecked ? time.GetUtcNow().UtcDateTime : null;
            sermon.CheckedBy = isChecked && context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
        }

        sermon.UpdatedAt = time.GetUtcNow().UtcDateTime;
        publisher.MarkAdmin(sermon.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await podcast.WriteAsync(cancellationToken);
        return Detail(sermon, await LookupsAsync(cancellationToken));
    }

    internal static void Correct(SermonRecord sermon, SermonCorrection request, bool speakerExists, bool seriesExists)
    {
        if (request.Title is { } title)
            sermon.Title = title.Trim() is { Length: > 0 and <= TitleLimit } trimmed ? trimmed : throw Invalid($"Give it a title of up to {TitleLimit} characters.");

        if (request.SpeakerId is { } speakerId)
            sermon.SpeakerId = speakerId.Length == 0 ? null
                : Guid.TryParse(speakerId, out Guid id) && speakerExists ? id
                : throw Invalid("Choose a speaker from the list.");

        if (request.SeriesId is { } seriesId)
            sermon.SeriesId = seriesId.Length == 0 ? null
                : Guid.TryParse(seriesId, out Guid id) && seriesExists ? id
                : throw Invalid("Choose a series from the list.");

        if (request.Service is { } service)
            sermon.Service = service.Trim().Length <= SermonServices.NameLimit ? service.Trim() : throw Invalid($"Keep the service to {SermonServices.NameLimit} characters.");

        if (request.YouTube is { } youTube)
            sermon.YouTubeId = youTube.Trim().Length == 0 ? null : YouTubeIds.Find(youTube) ?? throw Invalid("That isn't a YouTube link. Paste the video's address.");

        if (request.Passage is { } passage)
            sermon.Passage = SermonUploads.Passage(passage);

        if (request.Summary is { } summary)
            sermon.Summary = summary.Trim().Length <= SummaryLimit ? summary.Trim() : throw Invalid($"Keep the description to {SummaryLimit} characters.");

        if (request.Hidden is { } hidden)
            sermon.Hidden = hidden;
    }

    public async ValueTask<SeriesList> ListSeriesAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        ILookup<Guid?, SermonRecord> bySeries = sermons.ToLookup(sermon => sermon.SeriesId);
        List<SeriesRecord> series = await db.Series.AsNoTracking().ToListAsync(cancellationToken);
        return new SeriesList
        {
            Series = series.Select(record => SeriesDetail(record, bySeries[record.Id].ToList()))
                .OrderByDescending(detail => detail.LastAt ?? DateTime.MinValue).ThenBy(detail => detail.Name).ToList(),
        };
    }

    public async ValueTask<SeriesDetail> SaveSeriesAsync(SeriesDetail request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        if (request.Name.Trim() is not { Length: > 0 and <= NameLimit } name)
            throw Invalid($"Give the series a name of up to {NameLimit} characters.");
        if (request.Description.Length > DescriptionLimit)
            throw Invalid($"Keep the description to {DescriptionLimit} characters.");

        SeriesRecord? series = request.Id.Length == 0 ? null : await FindSeriesAsync(request.Id, cancellationToken);
        if (await db.Series.AnyAsync(other => other.Id != (series == null ? Guid.Empty : series.Id) && other.Name.ToLower() == name.ToLower(), cancellationToken))
            throw Invalid($"There's already a series called “{name}”.");
        if (series is null)
        {
            series = new SeriesRecord { Id = Guid.CreateVersion7(), Name = name, CreatedAt = time.GetUtcNow().UtcDateTime };
            db.Series.Add(series);
        }

        series.Name = name;
        series.Description = request.Description.Trim();
        series.Artwork = await VerifiedAsync(request.Artwork, SermonMedia.SeriesOwner(series.Id), cancellationToken);
        publisher.MarkAdmin(series.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        List<SermonRecord> visible = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        return SeriesDetail(series, visible.Where(sermon => sermon.SeriesId == series.Id).ToList());
    }

    public async ValueTask<SpeakerList> ListSpeakersAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        ILookup<Guid?, SermonRecord> bySpeaker = sermons.ToLookup(sermon => sermon.SpeakerId);
        List<SpeakerRecord> speakers = await db.Speakers.AsNoTracking().OrderBy(speaker => speaker.Name).ToListAsync(cancellationToken);
        return new SpeakerList { Speakers = speakers.Select(speaker => SpeakerDetail(speaker, bySpeaker[speaker.Id].Count())).ToList() };
    }

    public async ValueTask<SpeakerDetail> SaveSpeakerAsync(SpeakerDetail request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        SpeakerRecord speaker = request.Id.Length == 0
            ? new SpeakerRecord { Id = Guid.CreateVersion7(), Name = "", CreatedAt = time.GetUtcNow().UtcDateTime }
            : await db.Speakers.FindAsync([ParseId(request.Id, "speaker")], cancellationToken) ?? throw NotFound("That speaker");

        if (request.Name.Trim() is not { Length: > 0 and <= NameLimit } name)
            throw Invalid($"Give the speaker a name of up to {NameLimit} characters.");
        string key = Speakers.KeyOf(name);
        if ((await db.Speakers.Where(other => other.Id != speaker.Id).Select(other => other.Name).ToListAsync(cancellationToken)).Any(other => Speakers.KeyOf(other) == key))
            throw Invalid($"There's already a speaker called “{name}”.");
        bool isNew = db.Entry(speaker).State == EntityState.Detached;
        if (isNew)
            db.Speakers.Add(speaker);
        speaker.Name = name;
        speaker.Photo = await VerifiedAsync(request.Photo, SermonMedia.SpeakerOwner(speaker.Id), cancellationToken);

        publisher.MarkAdmin(speaker.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return SpeakerDetail(speaker, await CountForAsync(speaker.Id, cancellationToken));
    }

    public async ValueTask<Done> DeleteSpeakerAsync(DeleteRequest request, CallContext context = default)
    {
        await deletion.DeleteSpeakerAsync(ParseId(request.Id, "speaker"), MoveTo(request, "speaker"), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<SeriesDeletion> SeriesDeletionAsync(DeleteRequest request, CallContext context = default) =>
        await deletion.SeriesDeletionAsync(ParseId(request.Id, "series"), context.CancellationToken);

    public async ValueTask<Done> DeleteSeriesAsync(DeleteRequest request, CallContext context = default)
    {
        await deletion.DeleteSeriesAsync(ParseId(request.Id, "series"), MoveTo(request, "series"), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> DeleteSermonAsync(DeleteRequest request, CallContext context = default)
    {
        await deletion.DeleteSermonAsync(ParseId(request.Id, "sermon"), context.CancellationToken);
        return new Done();
    }

    private static Guid? MoveTo(DeleteRequest request, string what) => string.IsNullOrEmpty(request.MoveTo) ? null : ParseId(request.MoveTo, what);

    public async ValueTask<ServiceList> ListServicesAsync(Done request, CallContext context = default) => await services.ListAsync(context.CancellationToken);

    public async ValueTask<AudioUploadTicket> BeginAudioUploadAsync(AudioUploadRequest request, CallContext context = default) =>
        await uploads.BeginAsync(request, context.CancellationToken);

    public async ValueTask<SermonDetail> AddUploadAsync(SermonUpload request, CallContext context = default)
    {
        SermonRecord sermon = await uploads.AddAsync(request, context.CancellationToken);
        return Detail(sermon, await LookupsAsync(context.CancellationToken));
    }

    private async Task<int> CountForAsync(Guid speakerId, CancellationToken cancellationToken) =>
        await db.Sermons.CountAsync(sermon => !sermon.Hidden && sermon.SpeakerId == speakerId, cancellationToken);

    private async Task<MediaRef?> VerifiedAsync(MediaRef? claimed, string owner, CancellationToken cancellationToken)
    {
        IReadOnlyList<MediaRef> verified = claimed is null ? [] : await media.VerifyAsync([claimed], cancellationToken);
        await media.UseAsync(owner, verified, cancellationToken);
        return verified.FirstOrDefault();
    }

    private sealed record Lookups(Dictionary<Guid, SeriesRecord> Series, Dictionary<Guid, SpeakerRecord> Speakers, IReadOnlyDictionary<Guid, int>? SpeakerCounts = null);

    private async Task<IReadOnlyDictionary<Guid, int>> SpeakerCountsAsync(CancellationToken cancellationToken) =>
        (await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).Select(sermon => sermon.SpeakerId).ToListAsync(cancellationToken))
            .Where(id => id is not null).GroupBy(id => id!.Value).ToDictionary(group => group.Key, group => group.Count());

    private async Task<Lookups> LookupsAsync(CancellationToken cancellationToken) => new(
        await db.Series.AsNoTracking().ToDictionaryAsync(series => series.Id, cancellationToken),
        await db.Speakers.AsNoTracking().ToDictionaryAsync(speaker => speaker.Id, cancellationToken));

    private static SermonDetail Detail(SermonRecord sermon, Lookups lookups)
    {
        string? suggestion = sermon.SeriesId is null ? TitleParts.SuggestedSeries(sermon.Title) : null;
        return new SermonDetail
        {
            Sermon = SermonPublisher.Payload(sermon, lookups.Series, lookups.Speakers),
            Hidden = sermon.Hidden,
            SuggestedSeries = suggestion is null ? null
                : lookups.Series.Values.FirstOrDefault(series => string.Equals(series.Name, suggestion, StringComparison.OrdinalIgnoreCase))?.Name ?? suggestion,
            AddedAt = sermon.AddedAt,
            CheckedAt = sermon.CheckedAt,
            NeedsALook = sermon.CheckedAt is null && !sermon.Hidden ? ReasonsFor(sermon, lookups.SpeakerCounts) : [],
        };
    }

    internal static List<string> ReasonsFor(SermonRecord sermon, IReadOnlyDictionary<Guid, int>? speakerCounts)
    {
        List<string> reasons = [Reasons.JustUploaded];
        if (sermon.SeriesId is null && TitleParts.SuggestedSeries(sermon.Title) is not null)
            reasons.Add(Reasons.NoSeries);
        if (sermon.SpeakerId is { } speaker && speakerCounts?.GetValueOrDefault(speaker) == 1)
            reasons.Add(Reasons.NewSpeaker);
        return reasons;
    }

    private static SeriesDetail SeriesDetail(SeriesRecord series, IReadOnlyList<SermonRecord> sermons) => new()
    {
        Id = series.Id.ToString(),
        Name = series.Name,
        Description = series.Description,
        Artwork = series.Artwork,
        Count = sermons.Count,
        LastAt = sermons.Count == 0 ? null : sermons.Max(sermon => sermon.PreachedAt),
    };

    private static SpeakerDetail SpeakerDetail(SpeakerRecord speaker, int count) => new()
    {
        Id = speaker.Id.ToString(),
        Name = speaker.Name,
        Photo = speaker.Photo,
        Count = count,
    };

    private async Task<SermonRecord> FindSermonAsync(string id, CancellationToken cancellationToken) =>
        await db.Sermons.FindAsync([ParseId(id, "sermon")], cancellationToken) ?? throw NotFound("That sermon");

    private async Task<SeriesRecord> FindSeriesAsync(string id, CancellationToken cancellationToken) =>
        await db.Series.FindAsync([ParseId(id, "series")], cancellationToken) ?? throw NotFound("That series");

    private static Guid ParseId(string id, string what) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid($"That isn't a {what}.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException NotFound(string what) => new(new Status(StatusCode.NotFound, $"{what} has gone."));
}
