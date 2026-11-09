using SkiaSharp;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Tests;

internal static class SampleFiles
{
    public static string Text(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name));

    public static byte[] Mp3(byte variant)
    {
        const int FrameBytes = 417;
        byte[] audio = new byte[FrameBytes * 4];
        for (int frame = 0; frame < 4; frame++)
        {
            audio[frame * FrameBytes] = 0xFF;
            audio[frame * FrameBytes + 1] = 0xFB;
            audio[frame * FrameBytes + 2] = 0x90;
            audio[frame * FrameBytes + 3] = 0x00;
            audio[frame * FrameBytes + 4] = variant;
        }
        return audio;
    }

    public static byte[] Picture(int width, int height)
    {
        using SKBitmap bitmap = new(width, height);
        bitmap.Erase(new SKColor(0xFF2A3D5E));
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
