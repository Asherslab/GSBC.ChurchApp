using System.Buffers.Binary;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

internal static class SamplePictures
{
    public static byte[] Jpeg(int seed)
    {
        byte[] bytes = new byte[2048];
        new Random(seed).NextBytes(bytes);
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return bytes;
    }

    public static byte[] Png(int width, int height)
    {
        byte[] bytes = new byte[64];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 }.CopyTo(bytes, 0);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), (uint)height);
        return bytes;
    }

    public static readonly SKColor[] Corners = [SKColors.Red, SKColors.Lime, SKColors.Blue, SKColors.Black];

    public static byte[] Picture(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        using (SKCanvas canvas = new(bitmap))
        {
            canvas.Clear(SKColors.White);
            int corner = Math.Max(width, height) / 10;
            SKPoint[] origins = [new(0, 0), new(width - corner, 0), new(0, height - corner), new(width - corner, height - corner)];
            for (int index = 0; index < Corners.Length; index++)
            {
                using SKPaint paint = new();
                paint.Color = Corners[index];
                canvas.DrawRect(SKRect.Create(origins[index], new SKSize(corner, corner)), paint);
            }
        }
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    public static SKColor[] CornersOf(SKBitmap shown)
    {
        int inset = Math.Max(2, Math.Min(shown.Width, shown.Height) / 40);
        SKColor[] corners =
        [
            shown.GetPixel(inset, inset), shown.GetPixel(shown.Width - 1 - inset, inset),
            shown.GetPixel(inset, shown.Height - 1 - inset), shown.GetPixel(shown.Width - 1 - inset, shown.Height - 1 - inset),
        ];
        return corners.Select(Nearest).ToArray();
    }

    private static SKColor Nearest(SKColor color) =>
        Corners.Append(SKColors.White).MinBy(known => Math.Abs(known.Red - color.Red) + Math.Abs(known.Green - color.Green) + Math.Abs(known.Blue - color.Blue));

    public static byte[] WithOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif = [.. "Exif\0\0"u8, .. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2), .. exif, .. jpeg.AsSpan(2)];
    }

    public static byte[] JpegWithOrientation(int width, int height, ushort orientation)
    {
        List<byte> bytes = [0xFF, 0xD8];
        byte[] exif = [.. "Exif\0\0"u8, .. "II"u8, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0, 0, 0, 0, 0];
        bytes.AddRange([0xFF, 0xE1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2)]);
        bytes.AddRange(exif);
        bytes.AddRange([0xFF, 0xC0, 0, 17, 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 3]);
        bytes.AddRange(new byte[9]);
        bytes.AddRange([0xFF, 0xD9]);
        return [.. bytes];
    }
}
