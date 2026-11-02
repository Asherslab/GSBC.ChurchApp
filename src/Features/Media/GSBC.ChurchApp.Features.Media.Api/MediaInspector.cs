using System.Security.Cryptography;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed record InspectedContent(string Sha256, long Bytes, bool MatchesType);

internal static class MediaInspector
{
    private const int HeaderLength = 12;

    public static async Task<InspectedContent> InspectAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] header = new byte[HeaderLength];
        int headerRead = 0;
        long total = 0;
        byte[] buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (headerRead < HeaderLength)
            {
                int take = Math.Min(read, HeaderLength - headerRead);
                buffer.AsSpan(0, take).CopyTo(header.AsSpan(headerRead));
                headerRead += take;
            }

            hash.AppendData(buffer, 0, read);
            total += read;
        }

        return new InspectedContent(
            Convert.ToHexStringLower(hash.GetHashAndReset()), total, Matches(contentType.ToLowerInvariant(), header.AsSpan(0, headerRead)));
    }

    private static bool Matches(string contentType, ReadOnlySpan<byte> header) => contentType switch
    {
        "image/jpeg" => header.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
        "image/png" => header.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        "image/gif" => header.StartsWith("GIF8"u8),
        "image/webp" => header.StartsWith("RIFF"u8) && header.Length >= 12 && header[8..12].SequenceEqual("WEBP"u8),
        "application/pdf" => header.StartsWith("%PDF-"u8),
        _ => false,
    };
}
