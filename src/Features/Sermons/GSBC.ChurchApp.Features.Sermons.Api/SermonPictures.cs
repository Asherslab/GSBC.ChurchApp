using System.Text.RegularExpressions;
using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed partial class SermonPictures(
    SermonsDbContext db, IMediaReferences references, SermonPublisher publisher, ILogger<SermonPictures> logger) : IMediaHolder
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

    private async Task RefreshAsync(SermonRecord sermon, CancellationToken cancellationToken) =>
        sermon.Artwork = await CurrentAsync(sermon.Artwork, cancellationToken);

    private async Task RefreshAsync(SeriesRecord series, CancellationToken cancellationToken) =>
        series.Artwork = await CurrentAsync(series.Artwork, cancellationToken);

    private async Task RefreshAsync(SpeakerRecord speaker, CancellationToken cancellationToken) =>
        speaker.Photo = await CurrentAsync(speaker.Photo, cancellationToken);

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

    [GeneratedRegex("^sermons/(?<kind>sermon|series|speakers)/(?<id>[0-9a-f-]{36})(/|$)")]
    private static partial Regex OwnerPattern();
}
