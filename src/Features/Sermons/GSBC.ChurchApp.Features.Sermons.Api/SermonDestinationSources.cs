using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonDestinationSource(SermonsDbContext db) : IDestinationSource
{
    public DestinationKind Kind => SermonsDestinations.Sermon;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        string pattern = Patterns.Like(text);
        var found = await db.Sermons.AsNoTracking()
            .Where(sermon => !sermon.Hidden && sermon.PublishedHash != null)
            .Where(sermon => text.Length == 0 || EF.Functions.ILike(sermon.Title ?? sermon.SourceTitle, pattern) || EF.Functions.ILike(sermon.SourceSpeaker, pattern))
            .OrderByDescending(sermon => sermon.PreachedAt)
            .Take(take)
            .Select(sermon => new { sermon.Id, Title = sermon.Title ?? sermon.SourceTitle, sermon.PreachedAt, sermon.SourceSpeaker })
            .ToListAsync(cancellationToken);
        return found.Select(sermon => Place(sermon.Id, sermon.Title, sermon.SourceSpeaker, sermon.PreachedAt)).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid sermonId) && await db.Sermons.AsNoTracking().SingleOrDefaultAsync(sermon => sermon.Id == sermonId && !sermon.Hidden, cancellationToken) is { } found
            ? Place(found.Id, found.EffectiveTitle, found.SourceSpeaker, found.PreachedAt)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    private static FoundPlace Place(Guid id, string title, string speaker, DateTime preachedAt) =>
        new(id.ToString(), title, speaker.Length > 0 ? $"Sermon · {speaker}" : "Sermon", Audience.Everyone, When: preachedAt);
}

internal sealed class SeriesDestinationSource(SermonsDbContext db) : IDestinationSource
{
    public DestinationKind Kind => SermonsDestinations.Series;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        string pattern = Patterns.Like(text);
        List<SeriesRecord> found = await db.Series.AsNoTracking()
            .Where(series => series.PublishedHash != null && (text.Length == 0 || EF.Functions.ILike(series.Name, pattern)))
            .OrderByDescending(series => series.CreatedAt).Take(take).ToListAsync(cancellationToken);
        return found.Select(Place).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid seriesId) && await db.Series.AsNoTracking().SingleOrDefaultAsync(series => series.Id == seriesId, cancellationToken) is { } found
            ? Place(found)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    private static FoundPlace Place(SeriesRecord series) => new(series.Id.ToString(), series.Name, "Sermon series", Audience.Everyone);
}

internal sealed class SpeakerDestinationSource(SermonsDbContext db) : IDestinationSource
{
    public DestinationKind Kind => SermonsDestinations.Speaker;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        string pattern = Patterns.Like(text);
        List<SpeakerRecord> found = await db.Speakers.AsNoTracking()
            .Where(speaker => speaker.PublishedHash != null && (text.Length == 0 || EF.Functions.ILike(speaker.Name, pattern)))
            .OrderBy(speaker => speaker.Name).Take(take).ToListAsync(cancellationToken);
        return found.Select(Place).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid speakerId) && await db.Speakers.AsNoTracking().SingleOrDefaultAsync(speaker => speaker.Id == speakerId, cancellationToken) is { } found
            ? Place(found)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    private static FoundPlace Place(SpeakerRecord speaker) => new(speaker.Id.ToString(), speaker.Name, "Sermons by this speaker", Audience.Everyone);
}

internal static class Patterns
{
    public static string Like(string text) =>
        $"%{text.Replace("%", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal)}%";
}
