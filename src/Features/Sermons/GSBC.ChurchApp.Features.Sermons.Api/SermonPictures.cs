using System.Text.RegularExpressions;
using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed partial class SermonPictures(
    SermonsDbContext db, IMediaReferences references, IMediaSystemFolders folders, SermonPublisher publisher, ILogger<SermonPictures> logger) : IMediaHolder
{
    public bool Holds(string owner) => OwnerPattern().IsMatch(owner);

    public async Task RefreshAsync(string owner, CancellationToken cancellationToken)
    {
        if (OwnerPattern().Match(owner) is not { Success: true } match)
            return;
        Guid id = Guid.Parse(match.Groups["id"].Value);
        switch (match.Groups["kind"].Value)
        {
            case "sermon" when await db.Sermons.FindAsync([id], cancellationToken) is { } sermon:
                await RefreshAsync(sermon, cancellationToken);
                break;
            case "series" when await db.Series.FindAsync([id], cancellationToken) is { } series:
                await RefreshAsync(series, cancellationToken);
                break;
            case "speakers" when await db.Speakers.FindAsync([id], cancellationToken) is { } speaker:
                await RefreshAsync(speaker, cancellationToken);
                break;
            default:
                return;
        }
        publisher.MarkAdmin(id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    public async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        await NameByTheWebsiteAsync(cancellationToken);
        foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.Artwork != null).ToListAsync(cancellationToken))
            await RefreshAsync(sermon, cancellationToken);
        foreach (SeriesRecord series in await db.Series.Where(series => series.Artwork != null || series.ChosenArtwork != null).ToListAsync(cancellationToken))
            await RefreshAsync(series, cancellationToken);
        foreach (SpeakerRecord speaker in await db.Speakers.Where(speaker => speaker.Photo != null || speaker.ChosenPhoto != null).ToListAsync(cancellationToken))
            await RefreshAsync(speaker, cancellationToken);
        foreach (ArtworkSource source in await db.ArtworkSources.Where(source => source.Picture != null).ToListAsync(cancellationToken))
            source.Picture = await CurrentAsync(source.Picture, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    private async Task NameByTheWebsiteAsync(CancellationToken cancellationToken)
    {
        List<ArtworkSource> sources = await db.ArtworkSources.AsNoTracking().Where(source => source.Picture != null).ToListAsync(cancellationToken);
        Dictionary<string, string> addresses = (await db.ArtworkAddresses.AsNoTracking().OrderBy(address => address.Url).ToListAsync(cancellationToken))
            .GroupBy(address => address.Hash).ToDictionary(group => group.Key, group => group.First().Url);
        foreach (ArtworkSource source in sources)
        {
            if (!HashNamed().IsMatch(source.Picture!.FileName) || !addresses.TryGetValue(source.Hash, out string? url))
                continue;
            string named = Artwork.FileNameOf(url, source.Hash, Path.GetExtension(source.Picture.FileName).TrimStart('.'));
            if (named != source.Picture.FileName)
                await folders.NameAsync(Artwork.Area, source.Picture.Id, named, cancellationToken);
        }
    }

    private async Task RefreshAsync(SermonRecord sermon, CancellationToken cancellationToken) =>
        sermon.Artwork = await CurrentAsync(sermon.Artwork, cancellationToken);

    private async Task RefreshAsync(SeriesRecord series, CancellationToken cancellationToken)
    {
        series.Artwork = await CurrentAsync(series.Artwork, cancellationToken);
        series.ChosenArtwork = await CurrentAsync(series.ChosenArtwork, cancellationToken);
    }

    private async Task RefreshAsync(SpeakerRecord speaker, CancellationToken cancellationToken)
    {
        speaker.Photo = await CurrentAsync(speaker.Photo, cancellationToken);
        speaker.ChosenPhoto = await CurrentAsync(speaker.ChosenPhoto, cancellationToken);
    }

    private async Task<MediaRef?> CurrentAsync(MediaRef? saved, CancellationToken cancellationToken)
    {
        if (saved is null)
            return null;
        try
        {
            return (await references.VerifyAsync([saved], cancellationToken))[0];
        }
        catch (RpcException exception)
        {
            logger.LogWarning(exception, "Media no longer has {Media}; keeping what was saved", saved.Id);
            return saved;
        }
    }

    [GeneratedRegex("^[0-9a-f]{12}\\.[a-z]+$")]
    private static partial Regex HashNamed();

    [GeneratedRegex("^sermons/(?<kind>sermon|series|speakers)/(?<id>[0-9a-f-]{36})(/|$)")]
    private static partial Regex OwnerPattern();
}
