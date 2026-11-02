using System.Buffers.Binary;

namespace GSBC.ChurchApp.Features.Media.Api;

internal readonly record struct Dimensions(int Width, int Height)
{
    public static readonly Dimensions Unknown = new(0, 0);
}

internal static class ImageDimensions
{
    private const ushort OrientationTag = 0x0112;

    public static Dimensions Read(string contentType, ReadOnlySpan<byte> head)
    {
        try
        {
            return contentType switch
            {
                "image/png" => Png(head),
                "image/gif" => Gif(head),
                "image/webp" => WebP(head),
                "image/jpeg" => Jpeg(head),
                _ => Dimensions.Unknown,
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return Dimensions.Unknown;
        }
    }

    private static Dimensions Png(ReadOnlySpan<byte> head) =>
        head.Length >= 24 && head[12..16].SequenceEqual("IHDR"u8)
            ? new((int)BinaryPrimitives.ReadUInt32BigEndian(head[16..]), (int)BinaryPrimitives.ReadUInt32BigEndian(head[20..]))
            : Dimensions.Unknown;

    private static Dimensions Gif(ReadOnlySpan<byte> head) =>
        head.Length >= 10
            ? new(BinaryPrimitives.ReadUInt16LittleEndian(head[6..]), BinaryPrimitives.ReadUInt16LittleEndian(head[8..]))
            : Dimensions.Unknown;

    private static Dimensions WebP(ReadOnlySpan<byte> head)
    {
        if (head.Length < 30)
            return Dimensions.Unknown;

        ReadOnlySpan<byte> chunk = head[12..16];
        if (chunk.SequenceEqual("VP8 "u8))
            return new(BinaryPrimitives.ReadUInt16LittleEndian(head[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(head[28..]) & 0x3FFF);
        if (chunk.SequenceEqual("VP8L"u8))
        {
            uint bits = BinaryPrimitives.ReadUInt32LittleEndian(head[21..]);
            return new((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
        }
        if (chunk.SequenceEqual("VP8X"u8))
            return new(UInt24(head[24..]) + 1, UInt24(head[27..]) + 1);
        return Dimensions.Unknown;
    }

    private static Dimensions Jpeg(ReadOnlySpan<byte> head)
    {
        bool quarterTurned = false;
        int position = 2;
        while (position + 4 <= head.Length)
        {
            if (head[position] != 0xFF)
                return Dimensions.Unknown;

            byte marker = head[position + 1];
            if (marker == 0xFF)
            {
                position++;
                continue;
            }
            if (marker is 0x01 or >= 0xD0 and <= 0xD7)
            {
                position += 2;
                continue;
            }
            if (marker is 0xD9 or 0xDA)
                return Dimensions.Unknown;

            int length = BinaryPrimitives.ReadUInt16BigEndian(head[(position + 2)..]);
            ReadOnlySpan<byte> segment = head.Slice(position + 4, Math.Min(length - 2, head.Length - position - 4));

            if (IsStartOfFrame(marker))
            {
                int height = BinaryPrimitives.ReadUInt16BigEndian(segment[1..]);
                int width = BinaryPrimitives.ReadUInt16BigEndian(segment[3..]);
                return quarterTurned ? new(height, width) : new(width, height);
            }
            if (marker == 0xE1 && segment.StartsWith("Exif\0\0"u8))
                quarterTurned = ExifOrientation(segment[6..]) is >= 5 and <= 8;

            position += 2 + length;
        }

        return Dimensions.Unknown;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;

    private static int ExifOrientation(ReadOnlySpan<byte> tiff)
    {
        bool little = tiff.StartsWith("II"u8);
        if (!little && !tiff.StartsWith("MM"u8))
            return 0;

        int directory = (int)UInt32(tiff[4..], little);
        int entries = UInt16(tiff[directory..], little);
        for (int index = 0; index < entries; index++)
        {
            ReadOnlySpan<byte> entry = tiff.Slice(directory + 2 + index * 12, 12);
            if (UInt16(entry, little) == OrientationTag)
                return UInt16(entry[8..], little);
        }

        return 0;
    }

    private static int UInt16(ReadOnlySpan<byte> bytes, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes);

    private static uint UInt32(ReadOnlySpan<byte> bytes, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes);

    private static int UInt24(ReadOnlySpan<byte> bytes) => bytes[0] | bytes[1] << 8 | bytes[2] << 16;
}
