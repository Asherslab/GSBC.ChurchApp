namespace Gsbc.AppPoc.Layouts;

public enum AppLayout { Bulletin, Today, Hub, Simple, Week }

public sealed record NavTab(string Label, string Icon, string Href, params string[] Sections);

public sealed record LayoutOption(AppLayout Id, string Name, string Summary, bool ShowTabBar, IReadOnlyList<NavTab> Tabs)
{
    public string CssName => Id.ToString().ToLowerInvariant();
}

public static class LayoutCatalog
{
    private static readonly NavTab ListenTab = new("Listen", "headphones", "listen", "listen", "sermons");
    private static readonly NavTab PrayerTab = new("Prayer", "prayer", "prayer", "prayer");
    private static readonly NavTab MoreTab = new("More", "more", "more", "more", "visit", "appearance", "device", "notifications", "groups", "map");

    public static readonly IReadOnlyList<LayoutOption> All =
    [
        new(AppLayout.Bulletin, "Bulletin", "Sunday's bulletin is home. Five tabs: Sunday, Week, Listen, Prayer, More.", true,
        [
            new("Sunday", "bulletin", "", "", "sunday"),
            new("Week", "calendar", "week", "week", "events"),
            ListenTab, PrayerTab, MoreTab,
        ]),
        new(AppLayout.Today, "Today", "One home feed that changes with the week: what's on, Sunday and live, then the sermon replay.", true,
        [
            new("Today", "home", "", "", "sunday"),
            ListenTab,
            new("Calendar", "calendar", "week", "week", "events"),
            PrayerTab, MoreTab,
        ]),
        new(AppLayout.Hub, "Hub", "Home is a grid of large live tiles, like the old app's grid but showing real content.", true,
        [
            new("Home", "home", "", "", "sunday", "week", "events"),
            ListenTab, PrayerTab, MoreTab,
        ]),
        new(AppLayout.Simple, "Simple", "Big type and a few large buttons, no tab bar, a Home button everywhere.", false, []),
        new(AppLayout.Week, "The week", "Home is the calendar; Sunday's bulletin and new sermons sit on their day.", true,
        [
            new("Week", "calendar", "", "", "week", "events"),
            new("Sunday", "bulletin", "sunday", "sunday"),
            ListenTab, PrayerTab, MoreTab,
        ]),
    ];

    public static LayoutOption For(AppLayout id) => All.First(l => l.Id == id);
}
