using System.Security.Cryptography;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed record InspectedContent(string Sha256, long Bytes, bool MatchesType, Dimensions Dimensions);

internal static class MediaInspector
{
    private const int HeadLength = 256 * 1024;

    public static async Task<InspectedContent> InspectAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] head = new byte[HeadLength];
        int headRead = 0;
        long total = 0;
        byte[] buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (headRead < HeadLength)
            {
                int take = Math.Min(read, HeadLength - headRead);
                buffer.AsSpan(0, take).CopyTo(head.AsSpan(headRead));
                headRead += take;
            }

            hash.AppendData(buffer, 0, read);
            total += read;
        }

        string type = contentType.ToLowerInvariant();
        ReadOnlySpan<byte> start = head.AsSpan(0, headRead);
        bool matches = Matches(type, start);
        return new InspectedContent(
            Convert.ToHexStringLower(hash.GetHashAndReset()), total, matches, matches ? ImageDimensions.Read(type, start) : Dimensions.Unknown);
    }

    private static bool Matches(string contentType, ReadOnlySpan<byte> header) => contentType switch
    {
        "image/jpeg" => header.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
        "image/png" => header.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        "image/gif" => header.StartsWith("GIF8"u8),
        "image/webp" => header.StartsWith("RIFF"u8) && header.Length >= 12 && header[8..12].SequenceEqual("WEBP"u8),
        "application/pdf" => header.StartsWith("%PDF-"u8),
        "font/woff2" => header.StartsWith("wOF2"u8),
        "font/woff" => header.StartsWith("wOFF"u8),
        "font/ttf" => header.StartsWith((ReadOnlySpan<byte>)[0x00, 0x01, 0x00, 0x00]) || header.StartsWith("true"u8),
        "font/otf" => header.StartsWith("OTTO"u8),
        _ => false,
    };
}
