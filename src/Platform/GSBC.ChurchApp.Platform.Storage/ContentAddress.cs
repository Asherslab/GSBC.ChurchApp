namespace GSBC.ChurchApp.Platform.Storage;

public static class ContentAddress
{
    public const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    public static string KeyFor(string sha256Hex, string extension) => $"{sha256Hex}.{extension}";
}
