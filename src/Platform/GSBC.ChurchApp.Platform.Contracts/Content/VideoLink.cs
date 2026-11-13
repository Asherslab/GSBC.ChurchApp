namespace GSBC.ChurchApp.Platform.Contracts.Content;

public enum VideoSite
{
    YouTube,
    Vimeo,
}

public sealed record VideoLink(VideoSite Site, string Id)
{
    private const int YouTubeIdLength = 11;
    private const int LongestVimeoId = 12;

    public string PlayerUrl => Site == VideoSite.YouTube
        ? $"https://www.youtube-nocookie.com/embed/{Id}?rel=0&playsinline=1"
        : $"https://player.vimeo.com/video/{Id}?dnt=1";

    public string WatchUrl => Site == VideoSite.YouTube ? $"https://www.youtube.com/watch?v={Id}" : $"https://vimeo.com/{Id}";

    public string SiteName => Site == VideoSite.YouTube ? "YouTube" : "Vimeo";

    public static VideoLink? Of(string link)
    {
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort)
            return null;
        string host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        string[] path = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? id = host.ToLowerInvariant() switch
        {
            "youtu.be" when path.Length == 1 => path[0],
            "youtube.com" or "m.youtube.com" when path is ["watch"] => QueryValue(uri.Query, "v"),
            "youtube.com" or "m.youtube.com" when path is ["shorts" or "live" or "embed", _] => path[1],
            "vimeo.com" when path.Length == 1 => path[0],
            _ => null,
        };
        VideoSite site = host.Contains("vimeo", StringComparison.OrdinalIgnoreCase) ? VideoSite.Vimeo : VideoSite.YouTube;
        return id is not null && IsId(site, id) ? new VideoLink(site, id) : null;
    }

    private static bool IsId(VideoSite site, string id) => site == VideoSite.YouTube
        ? id.Length == YouTubeIdLength && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
        : id.Length <= LongestVimeoId && id.All(char.IsAsciiDigit);

    private static string? QueryValue(string query, string name)
    {
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == name)
                return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }
}
