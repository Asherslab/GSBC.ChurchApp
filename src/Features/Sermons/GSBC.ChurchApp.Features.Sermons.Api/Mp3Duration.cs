namespace GSBC.ChurchApp.Features.Sermons.Api;

internal static class Mp3Duration
{
    private const int HeadBytes = 64 * 1024;

    private static readonly int[,] Bitrates =
    {
        { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },
        { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
    };

    private static readonly int[,] SampleRates =
    {
        { 44100, 48000, 32000 },
        { 22050, 24000, 16000 },
        { 11025, 12000, 8000 },
    };

    public static async Task<int?> ReadAsync(Stream audio, long bytes, CancellationToken cancellationToken)
    {
        byte[] head = new byte[HeadBytes];
        int read = await audio.ReadAtLeastAsync(head, HeadBytes, throwOnEndOfStream: false, cancellationToken);
        int tag = TagBytes(head.AsSpan(0, read));
        if (tag == 0)
            return Seconds(head.AsSpan(0, read), bytes);
        return tag < read ? Seconds(head.AsSpan(tag, read - tag), bytes - tag) : null;
    }

    private static int TagBytes(ReadOnlySpan<byte> start) =>
        start.Length >= 10 && start[0] == 'I' && start[1] == 'D' && start[2] == '3'
            ? 10 + (start[6] << 21 | start[7] << 14 | start[8] << 7 | start[9]) + ((start[5] & 0x10) != 0 ? 10 : 0)
            : 0;

    public static int? Seconds(ReadOnlySpan<byte> audio, long audioBytes)
    {
        for (int offset = 0; offset + 4 <= audio.Length; offset++)
        {
            if (audio[offset] != 0xFF || (audio[offset + 1] & 0xE0) != 0xE0)
                continue;
            if (Header(audio[offset..]) is not { } header)
                continue;
            if (header.FrameBytes > 0 && offset + header.FrameBytes + 4 <= audio.Length
                && (audio[offset + header.FrameBytes] != 0xFF || (audio[offset + header.FrameBytes + 1] & 0xE0) != 0xE0))
                continue;

            if (FramesFromXing(audio[offset..], header) is { } frames and > 0)
                return (int)Math.Round((double)frames * header.SamplesPerFrame / header.SampleRate);
            if (header.Bitrate > 0)
                return (int)Math.Round((audioBytes - offset) * 8d / (header.Bitrate * 1000d));
            return null;
        }
        return null;
    }

    private sealed record FrameHeader(bool Mpeg1, bool Mono, int Bitrate, int SampleRate, int SamplesPerFrame, int FrameBytes);

    private static FrameHeader? Header(ReadOnlySpan<byte> frame)
    {
        int version = (frame[1] >> 3) & 0b11;
        int layer = (frame[1] >> 1) & 0b11;
        int bitrateIndex = frame[2] >> 4;
        int rateIndex = (frame[2] >> 2) & 0b11;
        if (version == 0b01 || layer != 0b01 || bitrateIndex is 0 or 15 || rateIndex == 0b11)
            return null;

        bool mpeg1 = version == 0b11;
        int bitrate = Bitrates[mpeg1 ? 0 : 1, bitrateIndex];
        int sampleRate = SampleRates[version switch { 0b11 => 0, 0b10 => 1, _ => 2 }, rateIndex];
        int samples = mpeg1 ? 1152 : 576;
        int padding = (frame[2] >> 1) & 1;
        bool mono = (frame[3] >> 6) == 0b11;
        int frameBytes = samples / 8 * bitrate * 1000 / sampleRate + padding;
        return new FrameHeader(mpeg1, mono, bitrate, sampleRate, samples, frameBytes);
    }

    private static long? FramesFromXing(ReadOnlySpan<byte> frame, FrameHeader header)
    {
        int side = header.Mpeg1 ? (header.Mono ? 17 : 32) : (header.Mono ? 9 : 17);
        int xing = 4 + side;
        if (frame.Length >= xing + 12 && (frame[xing..(xing + 4)].SequenceEqual("Xing"u8) || frame[xing..(xing + 4)].SequenceEqual("Info"u8)))
        {
            int flags = BigEndian(frame[(xing + 4)..]);
            return (flags & 1) != 0 ? BigEndian(frame[(xing + 8)..]) : null;
        }
        const int Vbri = 4 + 32;
        if (frame.Length >= Vbri + 18 && frame[Vbri..(Vbri + 4)].SequenceEqual("VBRI"u8))
            return BigEndian(frame[(Vbri + 14)..]);
        return null;
    }

    private static int BigEndian(ReadOnlySpan<byte> bytes) => bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
}
