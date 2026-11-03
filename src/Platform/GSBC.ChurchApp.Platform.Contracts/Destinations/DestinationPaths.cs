using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public static class DestinationPaths
{
    public const string SharedPrefix = "open";

    public static string Shareable(Uri appHost, string path) =>
        new Uri(appHost, Trim(path) is { Length: > 0 } route ? $"/{SharedPrefix}/{route}" : $"/{SharedPrefix}").ToString();

    public static string? RouteOf(string link)
    {
        if (LinkTargets.KindOf(link) == LinkKind.Web && Uri.TryCreate(link, UriKind.Absolute, out Uri? uri))
            link = uri.AbsolutePath;
        if (LinkTargets.KindOf(link) != LinkKind.InApp)
            return null;

        string route = Trim(link.Split('?', '#')[0]);
        return route == SharedPrefix ? "" : route.StartsWith(SharedPrefix + "/", StringComparison.OrdinalIgnoreCase) ? route[(SharedPrefix.Length + 1)..] : route;
    }

    public static string Trim(string path) => path.Trim('/');
}
