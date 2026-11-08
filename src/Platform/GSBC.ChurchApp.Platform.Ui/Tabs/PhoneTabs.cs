using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Configuration;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Ui.Tabs;

public sealed class PhoneTabs(ISurfaceLayout layout, ClientModules modules, IAccess access)
{
    public const string MoreRoute = "more";

    public event Action? Changed
    {
        add => layout.Changed += value;
        remove => layout.Changed -= value;
    }

    public async Task<IReadOnlyList<AppTab>> CurrentAsync() => TabBar.For(await layout.ShownAsync(TabBarSurfaces.Area));

    public IReadOnlyList<NavEntry> InMore(IReadOnlyList<AppTab> tabs) =>
        modules.Navigation
            .Where(entry => entry.Requires is null || access.CanSomewhere(entry.Requires))
            .Where(entry => !tabs.Any(tab => SameRoute(tab.Route, entry.Href)))
            .ToList();

    public bool IsOn(AppTab tab, string path)
    {
        string route = RouteOf(path);
        string section = FirstSegment(route);
        if (SameRoute(tab.Route, route))
            return true;
        if (tab.Route.Length == 0)
            return false;
        return (!tab.Route.Contains('/') && SameRoute(tab.Route, section))
            || modules.Navigation.Any(entry => SameRoute(entry.Href, tab.Route) && entry.AlsoUnder?.Contains(section, StringComparer.OrdinalIgnoreCase) == true);
    }

    public bool MoreIsOn(IReadOnlyList<AppTab> tabs, string path)
    {
        string route = RouteOf(path);
        if (tabs.Any(tab => IsOn(tab, path)))
            return false;
        string section = FirstSegment(route);
        return SameRoute(route, MoreRoute) || InMore(tabs).Any(entry => entry.Href.Length > 0 && SameRoute(FirstSegment(entry.Href), section));
    }

    private static string RouteOf(string path) => path.Split('?', '#')[0].Trim('/');

    private static string FirstSegment(string route) => route.Split('/')[0];

    private static bool SameRoute(string one, string other) => string.Equals(one.Trim('/'), other.Trim('/'), StringComparison.OrdinalIgnoreCase);
}
