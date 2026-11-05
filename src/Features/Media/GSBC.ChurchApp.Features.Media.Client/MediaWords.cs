using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Client;

internal static class MediaWords
{
    public static string NameOf(MediaRef media) => string.IsNullOrEmpty(media.FileName) ? media.Key : media.FileName;

    public static bool IsAudio(MediaRef media) => media.ContentType.StartsWith("audio/", StringComparison.Ordinal);

    public static string IconOf(MediaRef media) => IsAudio(media) ? "music" : "file";

    public static string UsedIn(int uses) => uses switch
    {
        0 => "Not used yet",
        1 => "Used in 1 place",
        _ => $"Used in {uses} places",
    };
}
