using System.Security.Cryptography;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed record ArtworkCopies(MediaRef Large, MediaRef Small);

internal sealed class Artwork(IBlobStore blobs)
{
    public const int LargeWidth = 1280;
    public const int SmallWidth = 480;
    public const int PhotoSize = 192;
    private const int JpegQuality = 82;

    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    public async Task<ArtworkCopies> CopiesAsync(byte[] original, string altText, CancellationToken cancellationToken)
    {
        using SKBitmap image = Decode(original);
        MediaRef large = await StoreAsync(Scaled(image, LargeWidth), altText, cancellationToken);
        MediaRef small = await StoreAsync(Scaled(image, SmallWidth), altText, cancellationToken);
        return new ArtworkCopies(large, small);
    }

    public async Task<MediaRef> PhotoAsync(byte[] original, string altText, CancellationToken cancellationToken)
    {
        using SKBitmap image = Decode(original);
        int side = Math.Min(image.Width, image.Height);
        SKBitmap square = new(PhotoSize, PhotoSize);
        using (SKCanvas canvas = new(square))
        using (SKImage source = SKImage.FromBitmap(image))
        {
            SKRect crop = SKRect.Create((image.Width - side) / 2f, (image.Height - side) / 2f, side, side);
            canvas.DrawImage(source, crop, SKRect.Create(PhotoSize, PhotoSize), Sampling);
        }
        return await StoreAsync(square, altText, cancellationToken);
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

    private async Task<MediaRef> StoreAsync(SKBitmap bitmap, string altText, CancellationToken cancellationToken)
    {
        byte[] bytes;
        int width = bitmap.Width, height = bitmap.Height;
        using (bitmap)
        using (SKImage image = SKImage.FromBitmap(bitmap))
        using (SKData data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality))
            bytes = data.ToArray();

        string key = ContentAddress.KeyFor(Convert.ToHexStringLower(SHA256.HashData(bytes)), "jpg");
        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
            await blobs.PutAsync(BlobBucket.Public, key, new MemoryStream(bytes), new BlobHeaders("image/jpeg", ContentAddress.ImmutableCacheControl), cancellationToken);
        return new MediaRef
        {
            Key = key,
            ContentType = "image/jpeg",
            Bytes = bytes.Length,
            Width = width,
            Height = height,
            Url = blobs.PublicUrlOf(key).ToString(),
            AltText = altText,
        };
    }
}
