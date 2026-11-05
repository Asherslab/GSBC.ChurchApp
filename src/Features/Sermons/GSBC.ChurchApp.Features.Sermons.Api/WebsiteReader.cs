using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class WebsiteReader(
    SermonsDbContext db, WebsiteClient website, Artwork artwork, Speakers speakers, SermonPublisher publisher, SermonAnnouncer announcer,
    IOptions<WebsiteOptions> options, TimeProvider time, ILogger<WebsiteReader> logger)
{
    public const string Origin = SermonSources.Website;

    public async Task ReadAsync(bool force, CancellationToken cancellationToken)
    {
        SourceState state = await SourceStates.GetAsync(db, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        bool changed;
        try
        {
            FetchedFeed feed = await website.FeedAsync(force ? null : state.ETag, force ? null : state.LastModified, cancellationToken);
            changed = feed.Changed;
            if (changed)
            {
                using MemoryStream body = new(feed.Body);
                await TakeAsync(PodcastFeed.Read(body), cancellationToken);
                state.ETag = feed.ETag;
                state.LastModified = feed.LastModified;
                state.ChangedAt = now;
            }
            state.CheckedAt = now;
            state.Problem = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Xml.XmlException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Reading the website's sermon feed failed");
            state.CheckedAt = now;
            state.Problem = Problem(exception);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        await db.SaveChangesAsync(cancellationToken);

        if (force || changed || state.SeriesCheckedAt is null || now - state.SeriesCheckedAt >= options.Value.SeriesEvery)
            await ReadSeriesAsync(state, cancellationToken);

        await FillInAsync(cancellationToken);
        if (!state.HasReadOnce)
        {
            state.HasReadOnce = true;
            await db.Sermons.Where(sermon => sermon.CheckedAt == null).ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.CheckedAt, now), cancellationToken);
            await db.Sermons.Where(sermon => sermon.AnnouncedAt == null).ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.AnnouncedAt, now), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        await announcer.AnnounceAsync(cancellationToken);
    }

    private async Task TakeAsync(IReadOnlyList<PodcastItem> items, CancellationToken cancellationToken)
    {
        List<string> guids = items.Select(item => item.Guid).ToList();
        Dictionary<string, SermonRecord> known = await db.Sermons.Where(sermon => sermon.ExternalId != null && guids.Contains(sermon.ExternalId))
            .ToDictionaryAsync(sermon => sermon.ExternalId!, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        string? fileHost = items.Select(item => Uri.TryCreate(item.AudioUrl, UriKind.Absolute, out Uri? uri) ? uri.Host : null).FirstOrDefault(host => host is not null);

        foreach (PodcastItem item in items)
        {
            if (!known.TryGetValue(item.Guid, out SermonRecord? sermon))
            {
                sermon = new SermonRecord { Id = Guid.CreateVersion7(), Origin = Origin, ExternalId = item.Guid, AddedAt = now };
                db.Sermons.Add(sermon);
            }

            sermon.SourceTitle = item.Title;
            sermon.WebLink = item.Link;
            sermon.PreachedAt = item.PublishedAt;
            sermon.SourceSpeaker = item.Author;
            sermon.SourceSpeakerId = item.Author.Length > 0 ? (await speakers.ResolveAsync(item.Author, cancellationToken)).Id : null;
            sermon.SourceService = ServiceLabels.FromSummary(item.Summary, item.Author);
            sermon.SourceYouTubeId = YouTubeIds.Find(item.Summary);
            sermon.Summary = item.Summary;
            if (sermon.AudioCopiedFrom is null && sermon.AudioUrl != item.AudioUrl)
            {
                sermon.AudioUrl = item.AudioUrl;
                sermon.DurationSeconds = 0;
            }
            sermon.AudioType = item.AudioType;
            if (sermon.AudioCopiedFrom is null)
                sermon.AudioBytes = item.AudioBytes;
            string? original = TithelyArtwork.OriginalOf(item.ImageUrl, fileHost);
            if (original != sermon.FeedArtworkUrl)
            {
                sermon.FeedArtworkUrl = original;
                sermon.PageReadAt = null;
                UseArtwork(sermon, original);
            }
            sermon.UpdatedAt = now;
        }
    }

    private static void UseArtwork(SermonRecord sermon, string? url)
    {
        if (url == sermon.ArtworkSourceUrl)
            return;
        sermon.ArtworkSourceUrl = url;
        sermon.ArtworkHash = null;
        sermon.Artwork = null;
        sermon.Thumbnail = null;
    }

    private async Task ReadPagesAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.Origin == Origin && sermon.PageReadAt == null && sermon.WebLink != null)
                     .OrderByDescending(sermon => sermon.PreachedAt).ToListAsync(cancellationToken))
            await Attempt(async () =>
            {
                string? fileHost = Uri.TryCreate(sermon.FeedArtworkUrl, UriKind.Absolute, out Uri? feedImage) ? feedImage.Host : null;
                if (await PageOfAsync(sermon.WebLink!, cancellationToken) is { } page && SermonPages.ArtworkOf(page, fileHost) is { } whole)
                    UseArtwork(sermon, whole);
                sermon.PageReadAt = now;
            }, "sermon page", sermon.WebLink!);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> PageOfAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await website.PageAsync(url, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
        {
            return null;
        }
    }

    private async Task ReadSeriesAsync(SourceState state, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        try
        {
            IReadOnlyList<SeriesLink> links = SeriesPages.ReadIndex(await website.PageAsync(SeriesPages.IndexPath, cancellationToken));
            if (links.Count == 0)
                throw new FormatException("The sermons page lists no series.");

            Dictionary<string, SeriesRecord> bySlug = await db.Series.Where(series => series.Slug != null).ToDictionaryAsync(series => series.Slug!, cancellationToken);
            Dictionary<string, Guid> seriesOfLink = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> photos = new(StringComparer.OrdinalIgnoreCase);
            foreach (SeriesLink link in links)
            {
                SeriesPage page = await ReadSeriesPageAsync(link, cancellationToken);
                if (!bySlug.TryGetValue(link.Slug, out SeriesRecord? series))
                {
                    series = new SeriesRecord { Id = Guid.CreateVersion7(), Slug = link.Slug, Name = link.Name, CreatedAt = now };
                    db.Series.Add(series);
                    bySlug[link.Slug] = series;
                }
                if (series.SourceName is not null && series.Name == series.SourceName)
                    series.Name = link.Name;
                series.SourceName = link.Name;
                if (page.ArtworkUrl != series.ArtworkSourceUrl)
                {
                    series.ArtworkSourceUrl = page.ArtworkUrl;
                    series.ArtworkHash = null;
                    series.Artwork = null;
                    series.Thumbnail = null;
                }
                foreach (string path in page.SermonPaths)
                    seriesOfLink.TryAdd(website.Absolute(path).ToString(), series.Id);
                foreach ((string name, string photo) in page.SpeakerPhotos)
                    photos.TryAdd(name, photo);
            }

            foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.Origin == Origin).ToListAsync(cancellationToken))
                sermon.SourceSeriesId = sermon.WebLink is not null && seriesOfLink.TryGetValue(sermon.WebLink, out Guid seriesId) ? seriesId : null;

            await speakers.TakePhotosAsync(photos, cancellationToken);
            state.SeriesCheckedAt = now;
            state.SeriesProblem = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or FormatException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Reading the website's series pages failed; keeping the series as they were");
            db.ChangeTracker.Entries<SeriesRecord>().Where(entry => entry.State == EntityState.Added).ToList().ForEach(entry => entry.State = EntityState.Detached);
            state.SeriesProblem = Problem(exception);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SeriesPage> ReadSeriesPageAsync(SeriesLink link, CancellationToken cancellationToken)
    {
        SeriesPage first = SeriesPages.ReadSeries(await website.PageAsync(SeriesPages.PathOf(link.Slug), cancellationToken), link.Name);
        List<string> sermons = [.. first.SermonPaths];
        Dictionary<string, string> photos = new(first.SpeakerPhotos, StringComparer.OrdinalIgnoreCase);
        foreach (int page in first.MorePages.Take(10))
        {
            SeriesPage more = SeriesPages.ReadSeries(await website.PageAsync($"{SeriesPages.PathOf(link.Slug)}?page={page}", cancellationToken), link.Name);
            sermons.AddRange(more.SermonPaths);
            foreach ((string name, string photo) in more.SpeakerPhotos)
                photos.TryAdd(name, photo);
        }
        return first with { SermonPaths = sermons.Distinct().ToList(), SpeakerPhotos = photos };
    }

    public async Task FillInAsync(CancellationToken cancellationToken)
    {
        await ReadPagesAsync(cancellationToken);
        foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.DurationSeconds == 0 && sermon.AudioUrl != "").ToListAsync(cancellationToken))
            await Attempt(async () => sermon.DurationSeconds = await DurationOfAsync(sermon, cancellationToken) ?? 0, "duration", sermon.AudioUrl);

        foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.ArtworkHash == null && sermon.ArtworkSourceUrl != null).OrderBy(sermon => sermon.AddedAt).ToListAsync(cancellationToken))
            await Attempt(async () =>
            {
                ArtworkCopies copies = await artwork.CopiesAsync(sermon.ArtworkSourceUrl!, sermon.EffectiveTitle, Artwork.SermonOwner(sermon.Id), cancellationToken);
                (sermon.ArtworkHash, sermon.Artwork, sermon.Thumbnail) = (copies.Hash, copies.Large, copies.Small);
            }, "artwork", sermon.ArtworkSourceUrl!);

        foreach (SeriesRecord series in await db.Series.Where(series => series.ArtworkHash == null && series.ArtworkSourceUrl != null).ToListAsync(cancellationToken))
            await Attempt(async () =>
            {
                ArtworkCopies copies = await artwork.CopiesAsync(series.ArtworkSourceUrl!, series.Name, Artwork.SeriesOwner(series.Id), cancellationToken);
                (series.ArtworkHash, series.Artwork, series.Thumbnail) = (copies.Hash, copies.Large, copies.Small);
            }, "series artwork", series.ArtworkSourceUrl!);

        await speakers.FillInPhotosAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    private async Task<int?> DurationOfAsync(SermonRecord sermon, CancellationToken cancellationToken)
    {
        byte[] head = await website.HeadOfAsync(sermon.AudioUrl, 0, cancellationToken);
        int tag = Mp3Duration.TagBytes(head);
        if (tag >= head.Length - 4)
            head = await website.HeadOfAsync(sermon.AudioUrl, tag, cancellationToken);
        else
            head = head[tag..];
        return Mp3Duration.Seconds(head, sermon.AudioBytes - tag);
    }

    private async Task Attempt(Func<Task> work, string what, string url)
    {
        try
        {
            await work();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException)
        {
            logger.LogWarning(exception, "Couldn't get the {What} from {Url}; trying again next time", what, url);
        }
    }

    private static string Problem(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: { } status } => $"The website answered {(int)status}.",
        HttpRequestException => "The website couldn't be reached.",
        TaskCanceledException => "The website took too long to answer.",
        System.Xml.XmlException => "The website's feed couldn't be read.",
        _ => exception.Message,
    };
}

internal static class SourceStates
{
    public static async Task<SourceState> GetAsync(SermonsDbContext db, CancellationToken cancellationToken)
    {
        if (await db.SourceStates.FindAsync([SourceState.SingleId], cancellationToken) is { } state)
            return state;
        state = new SourceState();
        db.SourceStates.Add(state);
        return state;
    }
}
