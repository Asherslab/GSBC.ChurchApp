using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaSizer(MediaDbContext db, IBlobStore blobs)
{
    public async Task SizeAsync(MediaItem item, Func<CancellationToken, Task<byte[]>> original, CancellationToken cancellationToken)
    {
        if (!item.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            item.Sizing = PictureSizing.NotAPicture;
            return;
        }

        if (await SizedTwinAsync(item, cancellationToken) is { } twin)
        {
            item.Sizing = twin.Sizing;
            foreach (MediaRendition rendition in await db.Renditions.AsNoTracking().Where(rendition => rendition.MediaId == twin.Id).ToListAsync(cancellationToken))
                db.Renditions.Add(CopyOf(rendition, item.Id));
            return;
        }

        PictureSizesMade made = PictureSizes.Make(await original(cancellationToken), item.ContentType);
        BlobBucket bucket = item.IsPrivate ? BlobBucket.Private : BlobBucket.Public;
        foreach (SizedPicture size in made.Sizes)
        {
            string sha256 = ContentAddress.Sha256Of(size.Bytes);
            string key = ContentAddress.KeyFor(sha256, size.Extension);
            if (await blobs.FindAsync(bucket, key, cancellationToken) is null)
                await blobs.PutAsync(bucket, key, new MemoryStream(size.Bytes), new BlobHeaders(size.ContentType, ContentAddress.ImmutableCacheControl), cancellationToken);
            db.Renditions.Add(new MediaRendition
            {
                MediaId = item.Id,
                Size = size.Name,
                Key = key,
                Sha256 = sha256,
                ContentType = size.ContentType,
                Bytes = size.Bytes.Length,
                Width = size.Width,
                Height = size.Height,
            });
        }
        item.Sizing = made.Sizing;
    }

    public Task SizeStoredAsync(MediaItem item, CancellationToken cancellationToken) =>
        SizeAsync(item, token => BytesOfAsync(blobs, item.IsPrivate ? BlobBucket.Private : BlobBucket.Public, item.Key, token), cancellationToken);

    public static async Task<byte[]> BytesOfAsync(IBlobStore blobs, BlobBucket bucket, string key, CancellationToken cancellationToken)
    {
        await using Stream stored = await blobs.OpenAsync(bucket, key, cancellationToken);
        using MemoryStream copy = new();
        await stored.CopyToAsync(copy, cancellationToken);
        return copy.ToArray();
    }

    private Task<MediaItem?> SizedTwinAsync(MediaItem item, CancellationToken cancellationToken) =>
        db.Items.AsNoTracking()
            .Where(other => other.Id != item.Id && other.Sha256 == item.Sha256 && other.ContentType == item.ContentType)
            .Where(other => (other.AudiencePermission == null) == (item.AudiencePermission == null))
            .Where(other => other.Sizing != PictureSizing.Waiting)
            .OrderBy(other => other.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static MediaRendition CopyOf(MediaRendition rendition, Guid mediaId) => new()
    {
        MediaId = mediaId,
        Size = rendition.Size,
        Key = rendition.Key,
        Sha256 = rendition.Sha256,
        ContentType = rendition.ContentType,
        Bytes = rendition.Bytes,
        Width = rendition.Width,
        Height = rendition.Height,
    };
}
