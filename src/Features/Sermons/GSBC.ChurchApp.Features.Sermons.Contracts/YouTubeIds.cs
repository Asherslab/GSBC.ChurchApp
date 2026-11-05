using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static partial class YouTubeIds
{
    public static string? Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        Match match = Link().Match(text);
        if (match.Success)
            return match.Groups["id"].Value;
        return Bare().IsMatch(text.Trim()) ? text.Trim() : null;
    }

    public static string WatchUrl(string id) => $"https://www.youtube.com/watch?v={id}";

    [GeneratedRegex(@"(?:youtu\.be/|youtube(?:-nocookie)?\.com/(?:watch\?(?:[^ ]*&)?v=|live/|embed/|shorts/))(?<id>[A-Za-z0-9_-]{11})")]
    private static partial Regex Link();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex Bare();
}
