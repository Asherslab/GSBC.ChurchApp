using System.Security.Cryptography;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed record ArtworkCopies(string Hash, MediaRef Large, MediaRef Small);

internal sealed record ArtworkPhoto(string Hash, MediaRef Photo);

internal sealed class Artwork(SermonsDbContext db, WebsiteClient website, IMediaSystemFolders folders, IMediaReferences references, TimeProvider time)
{
    public const int LargeWidth = 1280;
    public const int SmallWidth = 480;
    public const int PhotoSize = 192;
    private const int JpegQuality = 82;

    public static readonly MediaSystemArea Area = new("sermons", "Sermon artwork",
        "Pictures the app made from the website's sermon, series and speaker pictures. Each picture is kept once, however many sermons use it.",
        SermonsPermissions.Edit);

    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    public static string SermonOwner(Guid id) => $"sermons/sermon/{id}/artwork";

    public static string SeriesOwner(Guid id) => $"sermons/series/{id}/artwork";

    public static string SpeakerOwner(Guid id) => $"sermons/speakers/{id}/photo";

    public async Task<ArtworkCopies> CopiesAsync(string url, string altText, string owner, CancellationToken cancellationToken)
    {
        (ArtworkSource source, byte[]? bytes) = await SourceAsync(url, cancellationToken);
        if (source.Large is null || source.Small is null)
        {
            using SKBitmap image = Decode(bytes ?? await website.BytesAsync(url, cancellationToken));
            source.Large = await KeepAsync(Scaled(image, LargeWidth), source.Hash, "large", cancellationToken);
            source.Small = await KeepAsync(Scaled(image, SmallWidth), source.Hash, "small", cancellationToken);
        }
        await references.UseAsync(owner, [source.Large, source.Small], cancellationToken);
        return new ArtworkCopies(source.Hash, Described(source.Large, altText), Described(source.Small, altText));
    }

    public async Task<ArtworkPhoto> PhotoAsync(string url, string altText, string owner, CancellationToken cancellationToken)
    {
        (ArtworkSource source, byte[]? bytes) = await SourceAsync(url, cancellationToken);
        if (source.Photo is null)
        {
            using SKBitmap image = Decode(bytes ?? await website.BytesAsync(url, cancellationToken));
            source.Photo = await KeepAsync(Square(image), source.Hash, "photo", cancellationToken);
        }
        await references.UseAsync(owner, [source.Photo], cancellationToken);
        return new ArtworkPhoto(source.Hash, Described(source.Photo, altText));
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
    };

    private static SKBitmap Square(SKBitmap image)
    {
        int side = Math.Min(image.Width, image.Height);
        SKBitmap square = new(PhotoSize, PhotoSize);
        using SKCanvas canvas = new(square);
        using SKImage source = SKImage.FromBitmap(image);
        SKRect crop = SKRect.Create((image.Width - side) / 2f, (image.Height - side) / 2f, side, side);
        canvas.DrawImage(source, crop, SKRect.Create(PhotoSize, PhotoSize), Sampling);
        return square;
    }

    private static SKBitmap Decode(byte[] bytes)
    {
        using SKCodec codec = SKCodec.Create(new SKMemoryStream(bytes)) ?? throw new InvalidDataException("That isn't a picture.");
        SKBitmap decoded = SKBitmap.Decode(codec) ?? throw new InvalidDataException("That picture couldn't be read.");
        return Oriented(decoded, codec.EncodedOrigin);
    }

    private static SKBitmap Oriented(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return bitmap;
        bool swaps = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        SKBitmap turned = new(swaps ? bitmap.Height : bitmap.Width, swaps ? bitmap.Width : bitmap.Height);
        using (SKCanvas canvas = new(turned))
        {
            SKMatrix matrix = origin switch
            {
                SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, bitmap.Width, 0, 1, 0, 0, 0, 1),
                SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, bitmap.Width, 0, -1, bitmap.Height, 0, 0, 1),
                SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, bitmap.Height, 0, 0, 1),
                SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
                SKEncodedOrigin.RightTop => new SKMatrix(0, -1, bitmap.Height, 1, 0, 0, 0, 0, 1),
                SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, bitmap.Height, -1, 0, bitmap.Width, 0, 0, 1),
                _ => new SKMatrix(0, 1, 0, -1, 0, bitmap.Width, 0, 0, 1),
            };
            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(bitmap, 0, 0);
        }
        bitmap.Dispose();
        return turned;
    }

    private static SKBitmap Scaled(SKBitmap image, int width) =>
        image.Width <= width
            ? image.Copy()
            : image.Resize(new SKImageInfo(width, (int)Math.Round(image.Height * (double)width / image.Width)), Sampling);

    private async Task<MediaRef> KeepAsync(SKBitmap bitmap, string sourceHash, string size, CancellationToken cancellationToken)
    {
        byte[] bytes;
        int width = bitmap.Width, height = bitmap.Height;
        using (bitmap)
        using (SKImage image = SKImage.FromBitmap(bitmap))
        using (SKData data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality))
            bytes = data.ToArray();
        return await folders.KeepAsync(Area, new KeptFile(bytes, "image/jpeg", "jpg", $"{sourceHash[..12]}-{size}.jpg", width, height), cancellationToken);
    }
}
