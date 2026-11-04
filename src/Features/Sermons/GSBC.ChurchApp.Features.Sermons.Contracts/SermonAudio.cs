namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonAudio
{
    public const long LargestBytes = 300L * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensionByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audio/mpeg"] = "mp3",
        ["audio/mp3"] = "mp3",
        ["audio/mp4"] = "m4a",
        ["audio/x-m4a"] = "m4a",
        ["audio/aac"] = "m4a",
    };

    public static IEnumerable<string> Accepted => ExtensionByType.Keys;

    public static bool TryGetExtension(string contentType, out string extension) => ExtensionByType.TryGetValue(contentType, out extension!);

    public static string TypeOf(string extension) => extension == "m4a" ? "audio/mp4" : "audio/mpeg";
}
