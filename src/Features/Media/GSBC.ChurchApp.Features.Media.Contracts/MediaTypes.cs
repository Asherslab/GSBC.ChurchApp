namespace GSBC.ChurchApp.Features.Media.Contracts;

public static class MediaTypes
{
    public const long LargeFileBytes = 10 * 1024 * 1024;
    public const long LargeOriginalBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensionByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["image/gif"] = "gif",
        ["application/pdf"] = "pdf",
        ["font/woff2"] = "woff2",
        ["font/woff"] = "woff",
        ["font/ttf"] = "ttf",
        ["font/otf"] = "otf",
    };

    public static bool IsFont(string contentType) => contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase);

    public static string? FontTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".woff2" => "font/woff2",
        ".woff" => "font/woff",
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        _ => null,
    };

    public static IEnumerable<string> Accepted => ExtensionByType.Keys;

    public static bool TryGetExtension(string contentType, out string extension) =>
        ExtensionByType.TryGetValue(contentType, out extension!);

    public static bool IsLarge(long bytes) => bytes > LargeFileBytes;

    public static bool IsLargeOriginal(long bytes) => bytes > LargeOriginalBytes;

    public static bool IsResized(string contentType) => contentType.ToLowerInvariant() is "image/jpeg" or "image/png" or "image/webp";

    public static bool WarnsWhenLarge(string contentType, long bytes) => IsLarge(bytes) && !IsResized(contentType);
}
