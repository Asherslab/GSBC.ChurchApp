using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonLibraryService(
    SermonsDbContext db, SermonPublisher publisher, IMediaReferences media, WebsiteSchedule schedule, SermonUploads uploads,
    PodcastFeedWriter podcast, AudioCopier copier, TimeProvider time) : ISermonLibraryService
{
    public const int TitleLimit = 160;
    private const int ServiceLimit = 80;
    public const int NameLimit = 80;
    private const int DescriptionLimit = 600;

    public static class Filters
    {
        public const string All = "";
        public const string Corrected = "corrected";
        public const string Hidden = "hidden";
        public const string NeedsALook = "needs-a-look";
    }

    public static class Reasons
    {
        public const string JustUploaded = "Just uploaded";
        public const string NoSeries = "Part of a series?";
        public const string NewSpeaker = "New speaker";
        public const string New = "New";
    }

    public static class Fields
    {
        public const string Title = "title";
        public const string Speaker = "speaker";
        public const string Series = "series";
        public const string Service = "service";
        public const string YouTube = "youtube";
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
            matching = matching.Where(sermon => EF.Functions.ILike(sermon.Title ?? sermon.SourceTitle, pattern)
                || EF.Functions.ILike(sermon.Service ?? sermon.SourceService, pattern)
                || speakerIds.Contains(sermon.SpeakerId ?? sermon.SourceSpeakerId ?? Guid.Empty)
                || seriesIds.Contains((sermon.SeriesCorrected ? sermon.SeriesId : sermon.SourceSeriesId) ?? Guid.Empty));
        }

        Dictionary<string, int> counts = new()
        {
            [Filters.All] = await matching.CountAsync(sermon => !sermon.Hidden, cancellationToken),
            [Filters.Corrected] = await WithCorrections(matching).CountAsync(cancellationToken),
            [Filters.Hidden] = await matching.CountAsync(sermon => sermon.Hidden, cancellationToken),
            [Filters.NeedsALook] = await matching.CountAsync(sermon => !sermon.Hidden && sermon.CheckedAt == null, cancellationToken),
        };
        IQueryable<SermonRecord> filtered = request.Filter switch
        {
            Filters.Corrected => WithCorrections(matching),
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
            Total = counts[request.Filter is Filters.Corrected or Filters.Hidden or Filters.NeedsALook ? request.Filter : Filters.All],
            Counts = counts,
        };
    }

    public async ValueTask<SermonDetail> CorrectAsync(SermonCorrection request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        SermonRecord sermon = await FindSermonAsync(request.Id, cancellationToken);
        bool speakerExists = !request.UseSource.Contains(Fields.Speaker) && Guid.TryParse(request.SpeakerId, out Guid speakerId)
            && await db.Speakers.AnyAsync(speaker => speaker.Id == speakerId, cancellationToken);
        bool seriesExists = !request.UseSource.Contains(Fields.Series) && Guid.TryParse(request.SeriesId, out Guid seriesId)
            && await db.Series.AnyAsync(series => series.Id == seriesId, cancellationToken);
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
        await WritePodcastIfOursAsync(cancellationToken);
        return Detail(sermon, await LookupsAsync(cancellationToken));
    }

    internal static void Correct(SermonRecord sermon, SermonCorrection request, bool speakerExists, bool seriesExists)
    {
        HashSet<string> useSource = request.UseSource.ToHashSet();

        if (useSource.Contains(Fields.Title))
            sermon.Title = null;
        else if (request.Title is { } title)
            sermon.Title = title.Trim() is { Length: > 0 and <= TitleLimit } trimmed ? UnlessSource(trimmed, sermon.SourceTitle) : throw Invalid($"Give it a title of up to {TitleLimit} characters.");

        if (useSource.Contains(Fields.Speaker))
            sermon.SpeakerId = null;
        else if (request.SpeakerId is { } speakerId)
            sermon.SpeakerId = Guid.TryParse(speakerId, out Guid id) && speakerExists
                ? (id == sermon.SourceSpeakerId ? null : id)
                : throw Invalid("Choose a speaker from the list.");

        if (useSource.Contains(Fields.Series))
            (sermon.SeriesCorrected, sermon.SeriesId) = (false, null);
        else if (request.SeriesId is { } seriesId)
        {
            Guid? chosen = seriesId.Length == 0 ? null
                : Guid.TryParse(seriesId, out Guid id) && seriesExists ? id
                : throw Invalid("Choose a series from the list.");
            (sermon.SeriesCorrected, sermon.SeriesId) = chosen == sermon.SourceSeriesId && sermon.Origin == SermonSources.Website ? (false, null) : (true, chosen);
        }

        if (useSource.Contains(Fields.Service))
            sermon.Service = null;
        else if (request.Service is { } service)
            sermon.Service = service.Trim().Length <= ServiceLimit ? UnlessSource(service.Trim(), sermon.SourceService) : throw Invalid($"Keep the service to {ServiceLimit} characters.");

        if (useSource.Contains(Fields.YouTube))
            (sermon.YouTubeCorrected, sermon.YouTubeId) = (false, null);
        else if (request.YouTube is { } youTube)
        {
            string? id = youTube.Trim().Length == 0 ? null : YouTubeIds.Find(youTube) ?? throw Invalid("That isn't a YouTube link. Paste the video's address.");
            (sermon.YouTubeCorrected, sermon.YouTubeId) = id == sermon.SourceYouTubeId ? (false, null) : (true, id);
        }

        if (request.Hidden is { } hidden)
            sermon.Hidden = hidden;
    }

    internal static void MayMake(string source, bool isNew, string what)
    {
        if (isNew && source != SermonSources.Uploads)
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"While sermons come from the website, {what} come from it too. Switch Where new sermons come from to uploads to add them here."));
    }

    private async Task WritePodcastIfOursAsync(CancellationToken cancellationToken)
    {
        if ((await SourceStates.GetAsync(db, cancellationToken)).Source == SermonSources.Uploads)
            await podcast.WriteAsync(cancellationToken);
    }

    public async ValueTask<SeriesList> ListSeriesAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        ILookup<Guid?, SermonRecord> bySeries = sermons.ToLookup(sermon => sermon.EffectiveSeriesId);
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
        MayMake(await SourceStates.SourceAsync(db, cancellationToken), series is null, "series");
        if (series is null)
        {
            series = new SeriesRecord { Id = Guid.CreateVersion7(), Name = name, CreatedAt = time.GetUtcNow().UtcDateTime };
            db.Series.Add(series);
        }

        series.Name = name;
        series.Description = request.Description.Trim();
        series.ChosenArtwork = await VerifiedAsync(request.ArtworkChosen ? request.Artwork : null, Artwork.ChosenArtworkOwner(series.Id), cancellationToken);
        publisher.MarkAdmin(series.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        List<SermonRecord> visible = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        return SeriesDetail(series, visible.Where(sermon => sermon.EffectiveSeriesId == series.Id).ToList());
    }

    public async ValueTask<SpeakerList> ListSpeakersAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        ILookup<Guid?, SermonRecord> bySpeaker = sermons.ToLookup(sermon => sermon.EffectiveSpeakerId);
        ILookup<Guid, string> aliases = (await db.SpeakerAliases.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(alias => alias.SpeakerId, alias => alias.Alias);
        List<SpeakerRecord> speakers = await db.Speakers.AsNoTracking().OrderBy(speaker => speaker.Name).ToListAsync(cancellationToken);
        return new SpeakerList { Speakers = speakers.Select(speaker => SpeakerDetail(speaker, aliases[speaker.Id], bySpeaker[speaker.Id].Count())).ToList() };
    }

    public async ValueTask<SpeakerDetail> SaveSpeakerAsync(SpeakerDetail request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        SpeakerRecord speaker = request.Id.Length == 0
            ? new SpeakerRecord { Id = Guid.CreateVersion7(), Name = "", CreatedAt = time.GetUtcNow().UtcDateTime }
            : await db.Speakers.FindAsync([ParseId(request.Id, "speaker")], cancellationToken) ?? throw NotFound("That speaker");

        if (request.MergeInto is { Length: > 0 } mergeInto)
            return await MergeAsync(speaker, ParseId(mergeInto, "speaker"), cancellationToken);

        if (request.Name.Trim() is not { Length: > 0 and <= NameLimit } name)
            throw Invalid($"Give the speaker a name of up to {NameLimit} characters.");
        bool isNew = db.Entry(speaker).State == EntityState.Detached;
        MayMake(await SourceStates.SourceAsync(db, cancellationToken), isNew, "speakers");
        if (isNew)
            db.Speakers.Add(speaker);
        speaker.Name = name;
        speaker.ChosenPhoto = await VerifiedAsync(request.PhotoChosen ? request.Photo : null, Artwork.ChosenPhotoOwner(speaker.Id), cancellationToken);

        HashSet<string> wanted = request.Aliases.Append(name).Select(Speakers.AliasOf).Where(alias => alias.Length > 0).ToHashSet();
        List<SpeakerAlias> taken = await db.SpeakerAliases.Where(alias => wanted.Contains(alias.Alias) && alias.SpeakerId != speaker.Id).ToListAsync(cancellationToken);
        if (taken.FirstOrDefault() is { } clash)
            throw Invalid($"“{clash.Alias}” is another speaker's name. Merge the two instead.");
        List<SpeakerAlias> current = await db.SpeakerAliases.Where(alias => alias.SpeakerId == speaker.Id).ToListAsync(cancellationToken);
        db.SpeakerAliases.RemoveRange(current.Where(alias => !wanted.Contains(alias.Alias)));
        foreach (string alias in wanted.Except(current.Select(alias => alias.Alias)))
            db.SpeakerAliases.Add(new SpeakerAlias { Alias = alias, SpeakerId = speaker.Id });
        await ReattachAsync(speaker.Id, wanted, cancellationToken);

        publisher.MarkAdmin(speaker.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return SpeakerDetail(speaker, wanted, await CountForAsync(speaker.Id, cancellationToken));
    }

    public async ValueTask<SourceStatus> StatusAsync(Done request, CallContext context = default) => await StatusAsync(context.CancellationToken);

    public async ValueTask<SourceStatus> ChooseSourceAsync(SourceChoice request, CallContext context = default)
    {
        if (!SermonSources.IsKnown(request.Source))
            throw Invalid("Choose the website or uploads.");
        SourceState state = await SourceStates.GetAsync(db, context.CancellationToken);
        state.Source = request.Source;
        publisher.MarkAdmin(Guid.Empty);
        await db.SaveChangesAsync(context.CancellationToken);
        if (request.Source == SermonSources.Website)
            schedule.ReadSoon();
        else
            await podcast.WriteAsync(context.CancellationToken);
        return await StatusAsync(context.CancellationToken);
    }

    public async ValueTask<AudioUploadTicket> BeginAudioUploadAsync(AudioUploadRequest request, CallContext context = default) =>
        await uploads.BeginAsync(request, context.CancellationToken);

    public async ValueTask<SermonDetail> AddUploadAsync(SermonUpload request, CallContext context = default)
    {
        SermonRecord sermon = await uploads.AddAsync(request, context.CancellationToken);
        return Detail(sermon, await LookupsAsync(context.CancellationToken));
    }

    public async ValueTask<SourceStatus> CheckNowAsync(Done request, CallContext context = default)
    {
        schedule.ReadSoon();
        return await StatusAsync(context.CancellationToken);
    }

    private async Task<SourceStatus> StatusAsync(CancellationToken cancellationToken)
    {
        SourceState state = await SourceStates.GetAsync(db, cancellationToken);
        return new SourceStatus
        {
            Source = state.Source,
            CheckedAt = state.CheckedAt,
            ChangedAt = state.ChangedAt,
            SeriesCheckedAt = state.SeriesCheckedAt,
            Problem = state.Problem ?? state.SeriesProblem,
            Sermons = await db.Sermons.CountAsync(sermon => !sermon.Hidden, cancellationToken),
            PodcastFeedUrl = state.Source == SermonSources.Uploads ? podcast.Url : null,
        };
    }

    public async ValueTask<CopyProgress> CopyingAsync(Done request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        bool copying = (await SourceStates.GetAsync(db, cancellationToken)).Source == SermonSources.Uploads;
        List<SermonRecord> copied = await db.Sermons.AsNoTracking().Where(sermon => sermon.AudioCopiedFrom != null)
            .OrderByDescending(sermon => sermon.PreachedAt).ToListAsync(cancellationToken);
        List<SermonRecord> waiting = copying ? await AudioCopier.Waiting(db).AsNoTracking().ToListAsync(cancellationToken) : [];
        if (copier.Limit is { } limit)
            waiting = waiting.Take(Math.Max(0, limit - copied.Count)).ToList();
        return new CopyProgress
        {
            Items = [.. waiting.Select(CopyItem), .. copied.Select(CopyItem)],
            Copied = copied.Count,
            Total = copied.Count + waiting.Count,
            Note = copier.Limit is { } most && copying ? $"While developing, only the newest {most} are copied." : null,
        };
    }

    internal static CopyItem CopyItem(SermonRecord sermon) => new()
    {
        SermonId = sermon.Id.ToString(),
        Title = sermon.EffectiveTitle,
        PreachedAt = sermon.PreachedAt,
        State = sermon.AudioCopiedFrom is not null ? CopyStates.Copied
            : sermon.CopyStartedAt is not null ? CopyStates.Copying
            : sermon.CopyProblem is not null ? CopyStates.Failed
            : CopyStates.Waiting,
        Bytes = sermon.AudioBytes,
        CopiedBytes = sermon.AudioCopiedFrom is not null ? sermon.AudioBytes : sermon.CopiedBytes,
        Problem = sermon.AudioCopiedFrom is null ? sermon.CopyProblem : null,
    };

    private async Task<SpeakerDetail> MergeAsync(SpeakerRecord from, Guid intoId, CancellationToken cancellationToken)
    {
        if (from.Id == intoId)
            throw Invalid("Choose another speaker to merge into.");
        SpeakerRecord into = await db.Speakers.FindAsync([intoId], cancellationToken) ?? throw NotFound("That speaker");
        await db.SpeakerAliases.Where(alias => alias.SpeakerId == from.Id).ExecuteUpdateAsync(set => set.SetProperty(alias => alias.SpeakerId, into.Id), cancellationToken);
        await db.Sermons.Where(sermon => sermon.SourceSpeakerId == from.Id).ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.SourceSpeakerId, into.Id), cancellationToken);
        await db.Sermons.Where(sermon => sermon.SpeakerId == from.Id).ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.SpeakerId, into.Id), cancellationToken);
        bool takesChosen = into.ChosenPhoto is null && from.ChosenPhoto is not null;
        into.ChosenPhoto ??= from.ChosenPhoto;
        into.PhotoSourceUrl ??= from.PhotoSourceUrl;
        if (into.Photo is null && from.Photo is not null)
            (into.Photo, into.PhotoHash) = (from.Photo, null);
        await SourceIdMove.RememberAsync(db, from.Id, into.Id, FormerId.Speaker, time.GetUtcNow().UtcDateTime, settled: true, cancellationToken);
        from.Photo = from.ChosenPhoto = null;
        from.PhotoSourceUrl = from.PhotoHash = null;
        publisher.MarkAdmin(into.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await media.UseAsync(Artwork.SpeakerOwner(from.Id), [], cancellationToken);
        await media.UseAsync(Artwork.ChosenPhotoOwner(from.Id), [], cancellationToken);
        if (takesChosen)
            await media.UseAsync(Artwork.ChosenPhotoOwner(into.Id), [into.ChosenPhoto!], cancellationToken);
        db.Speakers.Remove(from);
        if (from.PublishedHash is not null)
            db.FeedChanges.Add(new Platform.Data.FeedChangeRecord { Feed = SermonsFeeds.Speakers, RecordId = from.Id.ToString(), Version = time.GetUtcNow().UtcDateTime.Ticks, Deleted = true });
        await db.SaveChangesAsync(cancellationToken);
        List<string> aliases = await db.SpeakerAliases.Where(alias => alias.SpeakerId == into.Id).Select(alias => alias.Alias).ToListAsync(cancellationToken);
        return SpeakerDetail(into, aliases, await CountForAsync(into.Id, cancellationToken));
    }

    private async Task ReattachAsync(Guid speakerId, IReadOnlySet<string> aliases, CancellationToken cancellationToken)
    {
        foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.SourceSpeaker != "").ToListAsync(cancellationToken))
            if (aliases.Contains(Speakers.AliasOf(sermon.SourceSpeaker)))
                sermon.SourceSpeakerId = speakerId;
    }

    private async Task<int> CountForAsync(Guid speakerId, CancellationToken cancellationToken) =>
        await db.Sermons.CountAsync(sermon => !sermon.Hidden && (sermon.SpeakerId ?? sermon.SourceSpeakerId) == speakerId, cancellationToken);

    private async Task<MediaRef?> VerifiedAsync(MediaRef? claimed, string owner, CancellationToken cancellationToken)
    {
        IReadOnlyList<MediaRef> verified = claimed is null ? [] : await media.VerifyAsync([claimed], cancellationToken);
        await media.UseAsync(owner, verified, cancellationToken);
        return verified.FirstOrDefault();
    }

    private sealed record Lookups(Dictionary<Guid, SeriesRecord> Series, Dictionary<Guid, SpeakerRecord> Speakers, IReadOnlyDictionary<Guid, int>? SpeakerCounts = null);

    private async Task<IReadOnlyDictionary<Guid, int>> SpeakerCountsAsync(CancellationToken cancellationToken) =>
        (await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).Select(sermon => sermon.SpeakerId ?? sermon.SourceSpeakerId).ToListAsync(cancellationToken))
            .Where(id => id is not null).GroupBy(id => id!.Value).ToDictionary(group => group.Key, group => group.Count());

    private async Task<Lookups> LookupsAsync(CancellationToken cancellationToken) => new(
        await db.Series.AsNoTracking().ToDictionaryAsync(series => series.Id, cancellationToken),
        await db.Speakers.AsNoTracking().ToDictionaryAsync(speaker => speaker.Id, cancellationToken));

    private static SermonDetail Detail(SermonRecord sermon, Lookups lookups)
    {
        string? suggestion = sermon.EffectiveSeriesId is null ? TitleParts.SuggestedSeries(sermon.EffectiveTitle) : null;
        return new SermonDetail
        {
            Sermon = SermonPublisher.Payload(sermon, lookups.Series, lookups.Speakers),
            Origin = sermon.Origin,
            Hidden = sermon.Hidden,
            Corrected = CorrectedFields(sermon),
            FromSource = new SermonFields
            {
                Title = sermon.SourceTitle,
                Speaker = sermon.SourceSpeakerId is { } id && lookups.Speakers.TryGetValue(id, out SpeakerRecord? speaker) ? speaker.Name : sermon.SourceSpeaker,
                SeriesId = sermon.SourceSeriesId?.ToString(),
                Service = sermon.SourceService,
                YouTubeId = sermon.SourceYouTubeId,
            },
            SuggestedSeries = suggestion is null ? null
                : lookups.Series.Values.FirstOrDefault(series => string.Equals(series.Name, suggestion, StringComparison.OrdinalIgnoreCase))?.Name ?? suggestion,
            AddedAt = sermon.AddedAt,
            CheckedAt = sermon.CheckedAt,
            NeedsALook = sermon.CheckedAt is null && !sermon.Hidden ? ReasonsFor(sermon, lookups.SpeakerCounts) : [],
        };
    }

    internal static List<string> CorrectedFields(SermonRecord sermon)
    {
        List<string> corrected = [];
        if (sermon.Title is not null) corrected.Add(Fields.Title);
        if (sermon.SpeakerId is not null) corrected.Add(Fields.Speaker);
        if (sermon.SeriesCorrected) corrected.Add(Fields.Series);
        if (sermon.Service is not null) corrected.Add(Fields.Service);
        if (sermon.YouTubeCorrected) corrected.Add(Fields.YouTube);
        return corrected;
    }

    internal static List<string> ReasonsFor(SermonRecord sermon, IReadOnlyDictionary<Guid, int>? speakerCounts)
    {
        List<string> reasons = [];
        if (sermon.Origin == SermonSources.Uploads)
            reasons.Add(Reasons.JustUploaded);
        if (sermon.EffectiveSeriesId is null && TitleParts.SuggestedSeries(sermon.EffectiveTitle) is not null)
            reasons.Add(Reasons.NoSeries);
        if (sermon.EffectiveSpeakerId is { } speaker && speakerCounts?.GetValueOrDefault(speaker) == 1)
            reasons.Add(Reasons.NewSpeaker);
        return reasons.Count > 0 ? reasons : [Reasons.New];
    }

    private static SeriesDetail SeriesDetail(SeriesRecord series, IReadOnlyList<SermonRecord> sermons) => new()
    {
        Id = series.Id.ToString(),
        Name = series.Name,
        Description = series.Description,
        Artwork = series.ChosenArtwork ?? series.Artwork,
        ArtworkChosen = series.ChosenArtwork is not null,
        Count = sermons.Count,
        LastAt = sermons.Count == 0 ? null : sermons.Max(sermon => sermon.PreachedAt),
        FromWebsite = series.Slug is not null,
    };

    private static SpeakerDetail SpeakerDetail(SpeakerRecord speaker, IEnumerable<string> aliases, int count) => new()
    {
        Id = speaker.Id.ToString(),
        Name = speaker.Name,
        Photo = speaker.ChosenPhoto ?? speaker.Photo,
        PhotoChosen = speaker.ChosenPhoto is not null,
        Aliases = aliases.Where(alias => alias != Speakers.AliasOf(speaker.Name)).Order().ToList(),
        Count = count,
    };

    private static IQueryable<SermonRecord> WithCorrections(IQueryable<SermonRecord> sermons) =>
        sermons.Where(sermon => sermon.Title != null || sermon.SpeakerId != null || sermon.SeriesCorrected || sermon.Service != null || sermon.YouTubeCorrected);

    private static string? UnlessSource(string value, string fromSource) => value == fromSource ? null : value;

    private async Task<SermonRecord> FindSermonAsync(string id, CancellationToken cancellationToken) =>
        await db.Sermons.FindAsync([ParseId(id, "sermon")], cancellationToken) ?? throw NotFound("That sermon");

    private async Task<SeriesRecord> FindSeriesAsync(string id, CancellationToken cancellationToken) =>
        await db.Series.FindAsync([ParseId(id, "series")], cancellationToken) ?? throw NotFound("That series");

    private static Guid ParseId(string id, string what) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid($"That isn't a {what}.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException NotFound(string what) => new(new Status(StatusCode.NotFound, $"{what} has gone."));
}
