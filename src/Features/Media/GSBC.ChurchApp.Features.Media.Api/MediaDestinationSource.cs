using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaDestinationSource(MediaDbContext db, IBlobStore blobs) : IDestinationSource
{
    public DestinationKind Kind => MediaDestinations.File;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        string pattern = $"%{text.Replace("%", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal)}%";
        List<MediaItem> items = await db.Items.AsNoTracking()
            .Where(item => !item.ContentType.StartsWith("font/"))
            .Where(item => text.Length == 0 || EF.Functions.ILike(item.FileName, pattern) || EF.Functions.ILike(item.AltText, pattern))
            .OrderByDescending(item => item.UploadedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return items.Select(Place).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid mediaId) && await db.Items.AsNoTracking().SingleOrDefaultAsync(stored => stored.Id == mediaId, cancellationToken) is { } item
            ? Place(item)
            : null;

    public async Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out Guid mediaId) || await db.Items.AsNoTracking().SingleOrDefaultAsync(stored => stored.Id == mediaId, cancellationToken) is not { } item)
            return null;
        return item.IsPrivate
            ? (await blobs.PresignDownloadAsync(BlobBucket.Private, item.Key, MediaLibrary.PrivateLinkLifetime)).ToString()
            : blobs.PublicUrlOf(item.Key).ToString();
    }

    private static FoundPlace Place(MediaItem item) => new(
        item.Id.ToString(),
        item.FileName.Length > 0 ? item.FileName : item.AltText.Length > 0 ? item.AltText : "Untitled file",
        Describe(item.ContentType),
        item.AudiencePermission is { } node ? Audience.Holding(node) : Audience.Everyone,
        item.AudienceScopeId,
        item.UploadedAt);

    private static string Describe(string contentType) => contentType switch
    {
        "application/pdf" => "PDF",
        _ when contentType.StartsWith("image/", StringComparison.Ordinal) => "Picture",
        _ when contentType.StartsWith("audio/", StringComparison.Ordinal) => "Audio",
        _ when contentType.StartsWith("video/", StringComparison.Ordinal) => "Video",
        _ => "File",
    };
}
