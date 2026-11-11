using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal static class MediaRemoval
{
    public static async Task DeleteUnlistedAsync(MediaDbContext db, IBlobStore blobs, bool isPrivate, IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        foreach (string key in keys.Distinct())
        {
            bool listed = await db.Items.AnyAsync(other => other.Key == key && other.OriginalRemovedAt == null && (other.AudiencePermission == null) != isPrivate, cancellationToken)
                || await db.Renditions.AnyAsync(rendition => rendition.Key == key
                    && db.Items.Any(other => other.Id == rendition.MediaId && (other.AudiencePermission == null) != isPrivate), cancellationToken);
            if (!listed)
                await blobs.DeleteAsync(isPrivate ? BlobBucket.Private : BlobBucket.Public, key, cancellationToken);
        }
    }
}
