namespace GSBC.ChurchApp.Platform.Storage;

public static class ContentAddress
{
    public const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    public const int Sha256HexLength = 64;

    public static string KeyFor(string sha256Hex, string extension) => $"{sha256Hex}.{extension}";

    public static bool IsSha256(string value) => value.Length == Sha256HexLength && value.All(char.IsAsciiHexDigitLower);
}
