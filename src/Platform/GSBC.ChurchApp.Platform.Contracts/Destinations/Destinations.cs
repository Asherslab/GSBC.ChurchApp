using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public sealed record DestinationArea(string Key, string Title, string Icon, int Order);

public sealed record DestinationPage(string Path, string Title, string Icon, DestinationArea Area, string Where, Audience Audience)
{
    public string Route => DestinationPaths.Trim(Path);
}

public sealed record DestinationKind(string Key, DestinationArea Area, string Noun, string Icon, string PathPrefix, bool OpensOutside = false, IReadOnlyList<string>? Feeds = null)
{
    public string PathOf(string id) => $"/{PathPrefix}/{Uri.EscapeDataString(id)}";
}

public sealed record DestinationSet(IReadOnlyList<DestinationPage> Pages, IReadOnlyList<DestinationKind> Kinds)
{
    public static DestinationSet Of(params IReadOnlyList<DestinationPage> pages) => new(pages, []);
}

public abstract record ResolvedDestination(string Path);

public sealed record ResolvedPage(DestinationPage Page) : ResolvedDestination(Page.Path);

public sealed record ResolvedItem(DestinationKind Kind, string Id) : ResolvedDestination(Kind.PathOf(Id));

public sealed class DestinationCatalog
{
    private readonly Dictionary<string, DestinationPage> _pages;
    private readonly Dictionary<string, DestinationKind> _kinds;

    public DestinationCatalog(IEnumerable<DestinationSet> sets)
    {
        List<DestinationSet> all = sets.ToList();
        Pages = all.SelectMany(set => set.Pages).ToList();
        Kinds = all.SelectMany(set => set.Kinds).ToList();

        _pages = new Dictionary<string, DestinationPage>(StringComparer.OrdinalIgnoreCase);
        foreach (DestinationPage page in Pages)
            if (!_pages.TryAdd(page.Route, page))
                throw new ArgumentException($"Two places in the app are declared at '/{page.Route}'.", nameof(sets));

        _kinds = new Dictionary<string, DestinationKind>(StringComparer.OrdinalIgnoreCase);
        foreach (DestinationKind kind in Kinds)
            if (!_kinds.TryAdd(kind.PathPrefix, kind) || _pages.ContainsKey(kind.PathPrefix))
                throw new ArgumentException($"'/{kind.PathPrefix}' is declared twice as a place in the app.", nameof(sets));

        Areas = Pages.Select(page => page.Area).Concat(Kinds.Select(kind => kind.Area))
            .DistinctBy(area => area.Key).OrderBy(area => area.Order).ToList();
    }

    public IReadOnlyList<DestinationPage> Pages { get; }

    public IReadOnlyList<DestinationKind> Kinds { get; }

    public IReadOnlyList<DestinationArea> Areas { get; }

    public DestinationKind? KindByKey(string key) => Kinds.FirstOrDefault(kind => kind.Key == key);

    public ResolvedDestination? Resolve(string link)
    {
        if (DestinationPaths.RouteOf(link) is not { } route)
            return null;
        if (_pages.TryGetValue(route, out DestinationPage? page))
            return new ResolvedPage(page);

        string[] segments = route.Split('/');
        return segments is [var prefix, { Length: > 0 } id] && _kinds.TryGetValue(prefix, out DestinationKind? kind)
            ? new ResolvedItem(kind, Uri.UnescapeDataString(id))
            : null;
    }
}
