using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

public sealed record WebsitePlace(string AppPath, string WebsitePath, bool LinksGoThere = false)
{
    public bool IsPrefix => AppPath.EndsWith('/');
}

public sealed class ChurchWebsite(NavigationManager navigation, IEnumerable<WebsitePlace> places)
{
    public string? AddressOf(string appPath) => Find(appPath) is { } found ? Address(found.Place, found.Tail) : null;

    public string? LinkGoesTo(string appPath) => Find(appPath) is { Place.LinksGoThere: true } found ? Address(found.Place, found.Tail) : null;

    private string? Address(WebsitePlace place, string rest) =>
        WebsiteHosts.OriginFor(new Uri(navigation.BaseUri)) is { } origin ? origin + place.WebsitePath + rest : null;

    private (WebsitePlace Place, string Tail)? Find(string appPath)
    {
        string path = "/" + appPath.Split('?', '#')[0].Trim('/');
        foreach (WebsitePlace place in places)
        {
            if (!place.IsPrefix && string.Equals(path, place.AppPath, StringComparison.OrdinalIgnoreCase))
                return (place, "");
            if (place.IsPrefix && path.StartsWith(place.AppPath, StringComparison.OrdinalIgnoreCase) && path[place.AppPath.Length..] is { Length: > 0 } rest && !rest.Contains('/'))
                return (place, rest);
        }
        return null;
    }
}

public static class ChurchWebsiteServiceCollectionExtensions
{
    public static IServiceCollection AddWebsitePlace(this IServiceCollection services, WebsitePlace place) =>
        services.AddSingleton(place);
}
