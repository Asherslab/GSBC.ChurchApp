using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui.Destinations;

public sealed class LinkOpener(NavigationManager navigation, DestinationsClient destinations, IAccess access, Shell shell)
{
    public const string UnavailablePath = "unavailable";

    public async Task OpenAsync(string link, bool replace = false)
    {
        link = link.Trim();
        if (OwnShareLink(link) is { } shared)
            link = shared;

        switch (LinkTargets.KindOf(link))
        {
            case LinkKind.Web or LinkKind.Email or LinkKind.Phone:
                await shell.OpenExternalAsync(link);
                return;
            case LinkKind.InApp:
                break;
            default:
                return;
        }

        switch (destinations.Catalog.Resolve(link))
        {
            case ResolvedPage { Page: var page } when page.Audience.Includes(Viewer):
                Go(InAppTarget(link), replace);
                break;
            case ResolvedPage { Page: var page }:
                Go(Unavailable(page.Path, DestinationState.NotForYou), replace);
                break;
            case ResolvedItem { Kind.OpensOutside: true } item:
                await OpenOutsideAsync(item.Path, replace);
                break;
            case ResolvedItem:
                Go(InAppTarget(link), replace);
                break;
            default:
                Go(Unavailable(DestinationPaths.RouteOf(link) is { } route ? "/" + route : link, DestinationState.Unknown), replace);
                break;
        }
    }

    public ResolvedDestination? Resolve(string link) => destinations.Catalog.Resolve(link);

    public async Task<Destination> OpenOutsideAsync(string path, bool replace = false)
    {
        Destination destination;
        try
        {
            destination = await destinations.DescribeAsync(path);
        }
        catch (Grpc.Core.RpcException)
        {
            destination = new Destination { Path = path, State = DestinationState.Unknown };
        }

        if (destination is { State: DestinationState.Available, OpenUrl: { Length: > 0 } url })
            await shell.OpenExternalAsync(url);
        else
            Go(Unavailable(path, destination.State), replace);
        return destination;
    }

    public static string Unavailable(string path, DestinationState state) =>
        $"{UnavailablePath}?to={Uri.EscapeDataString(path)}&why={state.ToString().ToLowerInvariant()}";

    private Viewer Viewer => new(access.SignedIn, node => access.Can(new Permission(node, ScopeKind.Global, "")));

    private string? OwnShareLink(string link) =>
        LinkTargets.KindOf(link) == LinkKind.Web && Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            && string.Equals(uri.Host, new Uri(navigation.BaseUri).Host, StringComparison.OrdinalIgnoreCase)
            ? uri.PathAndQuery
            : null;

    private static string InAppTarget(string link)
    {
        int query = link.IndexOfAny(['?', '#']);
        return (DestinationPaths.RouteOf(link) ?? "") + (query < 0 ? "" : link[query..]);
    }

    private void Go(string target, bool replace) =>
        navigation.NavigateTo(target.TrimStart('/'), new NavigationOptions { ReplaceHistoryEntry = replace });
}
