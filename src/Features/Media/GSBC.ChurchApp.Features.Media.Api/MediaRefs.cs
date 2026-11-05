using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal static class MediaRefs
{
    public const int MaximumTextLength = 200;

    public static MediaRef Of(MediaItem item, IReadOnlyCollection<MediaRendition> renditions, IBlobStore blobs)
    {
        List<MediaSize> sizes = renditions
            .OrderByDescending(rendition => rendition.Width)
            .Select(rendition => new MediaSize
            {
                Name = rendition.Size,
                Key = rendition.Key,
                ContentType = rendition.ContentType,
                Bytes = rendition.Bytes,
                Width = rendition.Width,
                Height = rendition.Height,
                Url = item.IsPrivate ? null : blobs.PublicUrlOf(rendition.Key).ToString(),
            })
            .ToList();
        MediaSize main = item.KeepsOriginal || sizes.Count == 0
            ? new MediaSize { Name = MediaSizeName.Original, Key = item.Key, ContentType = item.ContentType, Bytes = item.Bytes, Width = item.Width, Height = item.Height }
            : sizes[0];
        return new MediaRef
        {
            Id = item.Id.ToString(),
            Key = main.Key,
            ContentType = main.ContentType,
            Bytes = main.Bytes,
            Width = main.Width,
            Height = main.Height,
            Url = item.IsPrivate ? null : blobs.PublicUrlOf(main.Key).ToString(),
            IsPrivate = item.IsPrivate,
            AltText = item.AltText,
            FileName = item.FileName,
            Version = main.Name,
            Sizes = sizes.Where(size => size.Key != main.Key).ToList(),
        };
    }

    public static async Task<Dictionary<Guid, List<MediaRendition>>> RenditionsOfAsync(MediaDbContext db, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        List<Guid> wanted = [.. ids.Distinct()];
        List<MediaRendition> renditions = await db.Renditions.AsNoTracking().Where(rendition => wanted.Contains(rendition.MediaId)).ToListAsync(cancellationToken);
        return wanted.ToDictionary(id => id, id => renditions.Where(rendition => rendition.MediaId == id).ToList());
    }

    public static async Task<MediaRef> OfAsync(MediaDbContext db, MediaItem item, IBlobStore blobs, CancellationToken cancellationToken) =>
        Of(item, (await RenditionsOfAsync(db, [item.Id], cancellationToken))[item.Id], blobs);

    public static MediaOriginal OriginalOf(MediaItem item) => new()
    {
        ContentType = item.ContentType,
        Bytes = item.Bytes,
        Width = item.Width,
        Height = item.Height,
        Removed = !item.KeepsOriginal,
    };
}
