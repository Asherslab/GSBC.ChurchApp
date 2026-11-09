namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

internal static class SampleFiles
{
    public static byte[] ConstantBitrateMp3()
    {
        const int FrameBytes = 417;
        byte[] audio = new byte[FrameBytes * 4];
        for (int frame = 0; frame < 4; frame++)
        {
            audio[frame * FrameBytes] = 0xFF;
            audio[frame * FrameBytes + 1] = 0xFB;
            audio[frame * FrameBytes + 2] = 0x90;
            audio[frame * FrameBytes + 3] = 0x00;
        }
        return audio;
    }
}
