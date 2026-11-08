using GSBC.ChurchApp.Features.Events.Contracts;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Hosts.Website;

public sealed class WebsiteLinks(WebsiteOptions options)
{
    private static readonly (string Key, string Prefix)[] Items =
    [
        ("series", "/sermons/series/"),
        ("speaker", "/sermons/speakers/"),
        ("sermon", "/sermons/"),
        ("event", "/events/"),
    ];

    private static readonly (string View, string Path)[] Views = [("series", "/sermons/series"), ("speakers", "/sermons/speakers")];

    public string OnWebsite(string embedPathAndQuery)
    {
        Uri embed = new(new Uri("https://embed.invalid"), embedPathAndQuery);
        string path = embed.AbsolutePath;
        string page = path.StartsWith("/events", StringComparison.Ordinal) ? options.EventsPage : options.SermonsPage;

        Dictionary<string, string?> query = [];
        if (Views.FirstOrDefault(view => view.Path == path) is { View: { } name })
            query["view"] = name;
        else if (Items.FirstOrDefault(item => path.StartsWith(item.Prefix, StringComparison.Ordinal) && path.Length > item.Prefix.Length && !path[item.Prefix.Length..].Contains('/')) is { Key: { } key } found)
            query[key] = Uri.UnescapeDataString(path[found.Prefix.Length..]);

        foreach ((string key, Microsoft.Extensions.Primitives.StringValues values) in QueryHelpers.ParseQuery(embed.Query))
            query[key] = values.ToString();

        return QueryHelpers.AddQueryString(page, query);
    }

    public string InApp(string place) => $"{options.AppOrigin.TrimEnd('/')}/open/{place.TrimStart('/')}";

    public string Subscribe(string? calendarSlug) =>
        CalendarLinks.Subscribe(new Uri(options.AppOrigin), calendarSlug is { Length: > 0 } slug ? CalendarLinks.CalendarPath(slug) : CalendarLinks.PublicPath);

    public string Podcast => options.PodcastLink;
}
