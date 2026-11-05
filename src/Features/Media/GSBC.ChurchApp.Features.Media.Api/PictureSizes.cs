using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed record SizedPicture(MediaSizeName Name, byte[] Bytes, string ContentType, string Extension, int Width, int Height);

internal sealed record PictureSizesMade(PictureSizing Sizing, IReadOnlyList<SizedPicture> Sizes);

internal static class PictureSizes
{
    private const int Quality = 82;
    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    public static PictureSizesMade Make(byte[] original, string contentType)
    {
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return new PictureSizesMade(PictureSizing.NotAPicture, []);

        using SKCodec? codec = SKCodec.Create(new SKMemoryStream(original));
        if (codec is null || codec.FrameCount > 1 || !MediaTypes.IsResized(contentType))
            return new PictureSizesMade(PictureSizing.KeptAsIs, []);

        using SKBitmap? decoded = SKBitmap.Decode(codec);
        if (decoded is null)
            return new PictureSizesMade(PictureSizing.KeptAsIs, []);

        using SKBitmap upright = Upright(decoded, codec.EncodedOrigin);
        List<SizedPicture> sizes = [];
        foreach (MediaSizeName size in MediaSizes.Made)
        {
            int width = MediaSizes.WidthOf(size);
            if (upright.Width > width)
                sizes.Add(Encoded(size, Scaled(upright, width)));
        }
        return new PictureSizesMade(PictureSizing.Made, sizes);
    }

    private static SKBitmap Scaled(SKBitmap image, int width)
    {
        int height = Math.Max(1, (int)Math.Round(image.Height * (double)width / image.Width));
        return image.Resize(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul), Sampling);
    }

    private static SizedPicture Encoded(MediaSizeName size, SKBitmap bitmap)
    {
        using (bitmap)
        {
            bool opaque = IsOpaque(bitmap);
            using SKData data = bitmap.Encode(opaque ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Webp, Quality);
            return new SizedPicture(size, data.ToArray(), opaque ? "image/jpeg" : "image/webp", opaque ? "jpg" : "webp", bitmap.Width, bitmap.Height);
        }
    }

    private static bool IsOpaque(SKBitmap bitmap)
    {
        ReadOnlySpan<byte> pixels = bitmap.GetPixelSpan();
        for (int alpha = 3; alpha < pixels.Length; alpha += 4)
        {
            if (pixels[alpha] != byte.MaxValue)
                return false;
        }
        return true;
    }

    private static SKBitmap Upright(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        bool swaps = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        int width = swaps ? bitmap.Height : bitmap.Width, height = swaps ? bitmap.Width : bitmap.Height;
        SKBitmap upright = new(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using SKCanvas canvas = new(upright);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, width, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, width, -1, 0, height, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, height, 0, 0, 1),
            _ => SKMatrix.Identity,
        });
        canvas.DrawBitmap(bitmap, 0, 0);
        return upright;
    }
}
