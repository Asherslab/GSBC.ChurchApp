using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class TabBarSurfaces
{
    public const int MostChosenTabs = 3;
    public const int MostCharactersInAWord = 12;

    private const string EditNode = "config.tabs.edit";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Choose the app's tabs: which places are tabs, in what order, for whom and when", area: "App");

    private const string PublishNode = "config.tabs.publish";

    public static readonly Permission Publish =
        new(PublishNode, ScopeKind.Global, "Put changes to the app's tabs out to everyone, or on for an event", area: "App");

    public static readonly SurfaceArea Area = new("app.tabs", "Tab bar", "The tabs along the foot of the app. Home is always the first and More the last", ScopeKind.Global, Edit, Publish);

    public static readonly Surface Home = TabSettings.Declare("app.tab.home", new TabDefaults(ShellDestinations.Home.Path, "Home", "home"), defaultOrder: 100);

    public static readonly Surface News = TabSettings.Declare("app.tab.news", new TabDefaults("/news", "News", "megaphone"), defaultOrder: 150);

    public static readonly Surface Sermons = TabSettings.Declare("app.tab.sermons", new TabDefaults("/sermons", "Sermons", "sermons"), defaultOrder: 200);

    public static readonly Surface Tab = TabSettings.Declare("app.tab", new TabDefaults("", "", ""), defaultOrder: 1000, repeatable: true);

    public static readonly IReadOnlyList<Surface> StartingTabs = [Home, News, Sermons];

    public static SurfaceSet All => new([Home, News, Sermons, Tab]);

    public static bool IsTab(Surface surface) => surface.Area.Key == Area.Key;

    public static bool IsHome(Surface surface) => surface.Key == Home.Key;
}

public sealed record TabDefaults(string Opens, string Word, string Icon);

public static class TabSettings
{
    public static readonly PlaceSetting Opens = new("opens", "Opens");
    public static readonly TextSetting Word = new("word", "Word under the icon", required: true, maxLength: TabBarSurfaces.MostCharactersInAWord);
    public static readonly IconSetting Icon = new("icon", "Icon");

    public static Surface Declare(string key, TabDefaults defaults, int defaultOrder, bool repeatable = false) =>
        new(key, TabBarSurfaces.Area, "Tab", "A place in the app, as a tab", [
            new PlaceSetting(Opens.Key, Opens.Label, defaults.Opens),
            new TextSetting(Word.Key, Word.Label, defaults.Word, required: true, maxLength: TabBarSurfaces.MostCharactersInAWord),
            new IconSetting(Icon.Key, Icon.Label, defaults.Icon),
        ], defaultOrder: defaultOrder, repeatable: repeatable, check: NeedsAnIcon);

    private static IEnumerable<string> NeedsAnIcon(SettingValues values)
    {
        if (values.Get(Icon).Length == 0)
            yield return "A tab needs an icon.";
        if (IconSetting.IsCustom(values.Get(Icon)))
            yield return "A tab's icon has to be one of the app's own.";
    }
}

public sealed class PlaceSetting(string key, string label, string defaultValue = "", string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text) => text switch
    {
        "" => $"Choose what the tab {Label.ToLowerInvariant()}.",
        { Length: > LinkSetting.MostCharacters } => $"{Label} can be up to {LinkSetting.MostCharacters} characters.",
        _ when LinkTargets.KindOf(text) != LinkKind.InApp => "A tab opens a place in the app, not a web page.",
        _ when DestinationPaths.RouteOf(text) == ShellDestinations.More.Route => "More is always the last tab, so it can't be chosen again.",
        _ => null,
    };
}

public sealed record AppTab(string Key, string Opens, string Word, string Icon)
{
    public string Route => DestinationPaths.RouteOf(Opens) ?? "";
}

public static class TabBar
{
    public static IReadOnlyList<AppTab> Starting => For(SurfaceLayout.Arrange(TabBarSurfaces.StartingTabs, []));

    public static AppTab Home { get; } = Of(SurfaceLayout.Arrange([TabBarSurfaces.Home], [])[0]);

    public static IReadOnlyList<AppTab> For(IEnumerable<ArrangedSurface> shown) => [Home, .. Chosen(shown)];

    public static IReadOnlyList<AppTab> Chosen(IEnumerable<ArrangedSurface> shown) =>
        Candidates(shown)
            .Where(tab => !string.Equals(tab.Route, Home.Route, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(tab => tab.Route, StringComparer.OrdinalIgnoreCase)
            .Take(TabBarSurfaces.MostChosenTabs)
            .ToList();

    private static IReadOnlyList<AppTab> Candidates(IEnumerable<ArrangedSurface> shown) =>
        shown.Where(entry => TabBarSurfaces.IsTab(entry.Surface) && !TabBarSurfaces.IsHome(entry.Surface) && entry.Surface.ProblemsWith(entry.Values.Raw).Count == 0)
            .Select(Of)
            .ToList();

    public static AppTab Of(ArrangedSurface entry) => new(
        entry.Placement?.Id ?? entry.Surface.Key,
        entry.Values.Get(TabSettings.Opens),
        entry.Values.Get(TabSettings.Word),
        entry.Values.Get(TabSettings.Icon));
}
