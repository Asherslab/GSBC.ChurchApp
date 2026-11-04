using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Destinations;

public sealed class DestinationsService(DestinationDirectory directory) : IDestinationsService
{
    public async ValueTask<DestinationResults> SearchAsync(DestinationSearch request, CallContext context = default) =>
        await directory.SearchAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<Destination> DescribeAsync(DestinationRequest request, CallContext context = default) =>
        await directory.DescribeAsync(UserOf(context), request.Path, context.CancellationToken);

    private static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}

public sealed class DestinationDirectory(
    DestinationCatalog catalog, IEnumerable<IDestinationSource> sources, IAccessEvaluator access, PermissionCatalog permissions)
{
    private const int BrowseTake = 6;
    private const int AreaTake = 40;
    private const int MaximumTake = 100;

    public async Task<DestinationResults> SearchAsync(ClaimsPrincipal user, DestinationSearch request, CancellationToken cancellationToken)
    {
        string text = request.Text.Trim();
        int take = request.Take is > 0 ? Math.Min(request.Take, MaximumTake) : request.Area is null ? BrowseTake : AreaTake;
        Audience? openableBy = request.OpenableBy is { } audienceText && Audience.TryParse(audienceText, out Audience? parsed) ? parsed : null;

        DestinationResults results = new();
        foreach (DestinationArea area in catalog.Areas.Where(area => request.Area is null || area.Key == request.Area))
        {
            List<Destination> found = [];
            foreach (DestinationPage page in catalog.Pages.Where(page => page.Area == area && Matches(text, page.Title, page.Where)).OrderBy(page => page.Rank))
                found.Add(Describe(page, DestinationState.Available));
            foreach (IDestinationSource source in sources.Where(source => source.Kind.Area == area))
                foreach (FoundPlace place in await source.SearchAsync(text, take + 1, cancellationToken))
                    found.Add(Describe(source.Kind, place, DestinationState.Available));

            List<Destination> openable = [];
            foreach (Destination destination in found)
                if (await MayOpenAsync(user, destination, cancellationToken) && (openableBy is null || OpenableByAll(destination, openableBy)))
                    openable.Add(destination);

            if (openable.Count > 0 || request.Area is not null)
                results.Groups.Add(new DestinationGroup
                {
                    Area = area.Key, Title = text.Length == 0 && area.Browsing is { } browsing ? $"{area.Title} · {browsing}" : area.Title, Icon = area.Icon, Destinations = openable.Take(take).ToList(), HasMore = openable.Count > take,
                });
        }
        return results;
    }

    public async Task<Destination> DescribeAsync(ClaimsPrincipal user, string path, CancellationToken cancellationToken)
    {
        switch (catalog.Resolve(path))
        {
            case ResolvedPage { Page: var page }:
                Destination described = Describe(page, DestinationState.Available);
                return await MayOpenAsync(user, described, cancellationToken) ? described : Withheld(described.Path, DestinationState.NotForYou);
            case ResolvedItem { Kind: var kind, Id: var id } item when sources.FirstOrDefault(candidate => candidate.Kind == kind) is { } source:
                if (await source.FindAsync(id, cancellationToken) is not { } place)
                    return Withheld(item.Path, DestinationState.Gone);
                Destination destination = Describe(kind, place, DestinationState.Available);
                if (!await MayOpenAsync(user, destination, cancellationToken))
                    return Withheld(destination.Path, DestinationState.NotForYou);
                if (kind.OpensOutside)
                    destination.OpenUrl = await source.OpenUrlAsync(id, cancellationToken);
                return destination;
            default:
                return Withheld(path, DestinationState.Unknown);
        }
    }

    private static bool Matches(string text, params string[] fields) =>
        text.Length == 0 || fields.Any(field => field.Contains(text, StringComparison.OrdinalIgnoreCase));

    private static bool OpenableByAll(Destination destination, Audience viewers) =>
        Audience.TryParse(destination.Audience, out Audience? audience) && DestinationAudiences.OpenableByAll(audience, destination.AudienceScope, viewers);

    private async Task<bool> MayOpenAsync(ClaimsPrincipal user, Destination destination, CancellationToken cancellationToken)
    {
        if (!Audience.TryParse(destination.Audience, out Audience? audience))
            return false;
        if (audience == Audience.Everyone)
            return true;
        if (Caller.UserId(user) is null)
            return false;
        return audience.Node is not { } node || await access.IsAllowedAsync(user, node, destination.AudienceScope, cancellationToken);
    }

    private Destination Describe(DestinationPage page, DestinationState state) => new()
    {
        Path = page.Path, Title = page.Title, Where = page.Where, Icon = page.Icon, Area = page.Area.Key,
        Audience = page.Audience.Text, WhoCanOpen = WhoCanOpen(page.Audience), State = state,
    };

    private Destination Describe(DestinationKind kind, FoundPlace place, DestinationState state) => new()
    {
        Path = kind.PathOf(place.Id), Title = place.Title, Where = place.Where, Icon = kind.Icon, Area = kind.Area.Key, Kind = kind.Key,
        Audience = place.Audience.Text, AudienceScope = place.AudienceScope, WhoCanOpen = WhoCanOpen(place.Audience), When = place.When, State = state,
    };

    private static Destination Withheld(string path, DestinationState state) => new() { Path = path, State = state };

    private string WhoCanOpen(Audience audience) => audience switch
    {
        _ when audience == Audience.Everyone => "Everyone",
        { Node: null } => "Everyone signed in",
        { Node: { } node } when permissions.ByNode.TryGetValue(node, out var permission) => $"Only people allowed to {Lower(permission.Description)}",
        _ => "Only some people",
    };

    private static string Lower(string text) => text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
}
