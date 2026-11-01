using System.Security.Cryptography;

namespace GSBC.ChurchApp.Platform.Storage;

public static class ContentAddress
{
    public static async Task<string> ForAsync(Stream content, CancellationToken cancellationToken)
    {
        byte[] hash = await SHA256.HashDataAsync(content, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
