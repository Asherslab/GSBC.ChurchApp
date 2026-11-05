using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaHolders(MediaDbContext db, IEnumerable<IMediaHolder> holders, ILogger<MediaHolders> logger)
{
    public async Task<bool> CanAllRefreshAsync(Guid mediaId, CancellationToken cancellationToken) =>
        (await OwnersOfAsync([mediaId], cancellationToken)).All(owner => HolderOf(owner) is not null);

    public async Task RefreshAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken)
    {
        foreach (string owner in await OwnersOfAsync(mediaIds, cancellationToken))
        {
            if (HolderOf(owner) is not { } holder)
                continue;
            try
            {
                await holder.RefreshAsync(owner, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Couldn't give {Owner} the current sizes of its pictures", owner);
            }
        }
    }

    private IMediaHolder? HolderOf(string owner) => holders.FirstOrDefault(holder => holder.Holds(owner));

    private Task<List<string>> OwnersOfAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken) =>
        db.Uses.AsNoTracking().Where(use => mediaIds.Contains(use.MediaId)).Select(use => use.Owner).Distinct().ToListAsync(cancellationToken);
}
