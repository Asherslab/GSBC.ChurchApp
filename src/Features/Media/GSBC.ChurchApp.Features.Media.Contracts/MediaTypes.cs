namespace GSBC.ChurchApp.Features.Media.Contracts;

public static class MediaTypes
{
    public const long LargeFileBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensionByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["image/gif"] = "gif",
        ["application/pdf"] = "pdf",
    };

    public static IEnumerable<string> Accepted => ExtensionByType.Keys;

    public static bool TryGetExtension(string contentType, out string extension) =>
        ExtensionByType.TryGetValue(contentType, out extension!);

    public static bool IsLarge(long bytes) => bytes > LargeFileBytes;
}
