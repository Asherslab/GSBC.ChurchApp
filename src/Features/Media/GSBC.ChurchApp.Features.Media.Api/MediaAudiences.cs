using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaAudiences(MediaDbContext db, IBlobStore blobs, TimeProvider time) : IMediaAudiences
{
    public async Task<bool> ShareAsync(MediaSystemArea area, string mediaId, string owner, string? permissionNode, string? scopeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(mediaId, out Guid id) || await db.Items.SingleOrDefaultAsync(item => item.Id == id && item.Area == area.Key, cancellationToken) is not { } item)
            return false;
        if (await db.Uses.AnyAsync(use => use.MediaId == id && use.Owner != owner, cancellationToken))
            return false;

        bool wasPrivate = item.IsPrivate;
        (item.AudiencePermission, item.AudienceScopeId) = (permissionNode, permissionNode is null ? null : scopeId);
        List<string> keys = [];
        if (wasPrivate != item.IsPrivate)
        {
            (BlobBucket from, BlobBucket to) = wasPrivate ? (BlobBucket.Private, BlobBucket.Public) : (BlobBucket.Public, BlobBucket.Private);
            List<(string Key, string ContentType)> stored = (await db.Renditions.AsNoTracking().Where(rendition => rendition.MediaId == id)
                    .Select(rendition => new { rendition.Key, rendition.ContentType }).ToListAsync(cancellationToken))
                .Select(each => (each.Key, each.ContentType)).ToList();
            if (item.KeepsOriginal)
                stored.Add((item.Key, item.ContentType));
            foreach ((string key, string contentType) in stored.Distinct())
            {
                await blobs.CopyAsync(from, key, to, key, new BlobHeaders(contentType, ContentAddress.ImmutableCacheControl), cancellationToken);
                keys.Add(key);
            }
        }
        db.FeedChanges.Add(FeedChangeRecord.Marker(MediaFeeds.Library, item.Id.ToString(), time.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(cancellationToken);
        await MediaRemoval.DeleteUnlistedAsync(db, blobs, wasPrivate, keys, cancellationToken);
        return true;
    }
}
