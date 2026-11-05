using System.Security.Cryptography;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed record ArtworkPicture(string Hash, MediaRef Picture);

internal sealed class Artwork(SermonsDbContext db, WebsiteClient website, IMediaSystemFolders folders, IMediaReferences references, TimeProvider time)
{
    public static readonly MediaSystemArea Area = new("sermons", "Sermon artwork",
        "The website's sermon, series and speaker pictures, as the website has them. Each picture is kept once, however many sermons use it, and Media makes the sizes phones need.",
        SermonsPermissions.Edit);

    public static string SermonOwner(Guid id) => $"sermons/sermon/{id}/artwork";

    public static string SeriesOwner(Guid id) => $"sermons/series/{id}/artwork";

    public static string SpeakerOwner(Guid id) => $"sermons/speakers/{id}/photo";

    public static string ChosenArtworkOwner(Guid id) => $"sermons/series/{id}";

    public static string ChosenPhotoOwner(Guid id) => $"sermons/speakers/{id}";

    public async Task<ArtworkPicture> PictureAsync(string url, string altText, string owner, CancellationToken cancellationToken)
    {
        (ArtworkSource source, byte[]? bytes) = await SourceAsync(url, cancellationToken);
        source.Picture = source.Picture is { } kept
            ? (await references.VerifyAsync([kept], cancellationToken))[0]
            : await KeepAsync(bytes ?? await website.BytesAsync(url, cancellationToken), url, source.Hash, cancellationToken);
        await references.UseAsync(owner, [source.Picture], cancellationToken);
        return new ArtworkPicture(source.Hash, Described(source.Picture, altText));
    }

    public async Task<string?> LargestAsync(IReadOnlyList<string> urls, CancellationToken cancellationToken)
    {
        string? largest = null;
        long most = 0;
        foreach (string url in urls)
        {
            using SKCodec? codec = SKCodec.Create(new SKMemoryStream(await website.BytesAsync(url, cancellationToken)));
            if (codec is not null && (long)codec.Info.Width * codec.Info.Height > most)
                (largest, most) = (url, (long)codec.Info.Width * codec.Info.Height);
        }
        return largest;
    }

    public Task ReleaseAsync(string owner, CancellationToken cancellationToken) => references.UseAsync(owner, [], cancellationToken);

    private async Task<(ArtworkSource Source, byte[]? Bytes)> SourceAsync(string url, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        ArtworkAddress? known = await db.ArtworkAddresses.FindAsync([url], cancellationToken);
        byte[]? bytes = null;
        if (known is null || known.ETag is not null || known.LastModified is not null)
        {
            FetchedFeed fetched = await website.IfChangedAsync(new Uri(url), known?.ETag, known?.LastModified, cancellationToken);
            if (fetched.Changed)
            {
                bytes = fetched.Body;
                string hash = HashOf(bytes);
                if (known is null)
                    known = db.ArtworkAddresses.Add(new ArtworkAddress { Url = url, Hash = hash }).Entity;
                (known.Hash, known.ETag, known.LastModified) = (hash, fetched.ETag, fetched.LastModified);
            }
            known!.FetchedAt = now;
        }

        ArtworkSource source = await db.ArtworkSources.FindAsync([known.Hash], cancellationToken)
            ?? db.ArtworkSources.Add(new ArtworkSource { Hash = known.Hash, AddedAt = now }).Entity;
        return (source, bytes);
    }

    public static string HashOf(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static MediaRef Described(MediaRef kept, string altText) => new()
    {
        Id = kept.Id,
        Key = kept.Key,
        ContentType = kept.ContentType,
        Bytes = kept.Bytes,
        Width = kept.Width,
        Height = kept.Height,
        Url = kept.Url,
        FileName = kept.FileName,
        AltText = altText,
        Version = kept.Version,
        Sizes = kept.Sizes,
    };

    public static string FileNameOf(string url, string sourceHash, string extension)
    {
        string named = Uri.TryCreate(url, UriKind.Absolute, out Uri? address) ? Uri.UnescapeDataString(address.Segments[^1]).Trim('/') : "";
        string stem = Path.GetFileNameWithoutExtension(named);
        return stem.Length is > 0 and <= 80 && stem.All(letter => char.IsLetterOrDigit(letter) || letter is '-' or '_' or '.' or ' ')
            ? $"{stem}.{extension}"
            : $"{sourceHash[..12]}.{extension}";
    }

    private async Task<MediaRef> KeepAsync(byte[] bytes, string url, string sourceHash, CancellationToken cancellationToken)
    {
        using SKCodec codec = SKCodec.Create(new SKMemoryStream(bytes)) ?? throw new InvalidDataException("That isn't a picture.");
        (string contentType, string extension) = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => ("image/jpeg", "jpg"),
            SKEncodedImageFormat.Png => ("image/png", "png"),
            SKEncodedImageFormat.Webp => ("image/webp", "webp"),
            SKEncodedImageFormat.Gif => ("image/gif", "gif"),
            _ => throw new InvalidDataException("That picture isn't a JPEG, PNG, WebP or GIF."),
        };
        bool turned = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        (int width, int height) = turned ? (codec.Info.Height, codec.Info.Width) : (codec.Info.Width, codec.Info.Height);
        return await folders.KeepAsync(Area, new KeptFile(bytes, contentType, extension, FileNameOf(url, sourceHash, extension), width, height), cancellationToken);
    }
}
