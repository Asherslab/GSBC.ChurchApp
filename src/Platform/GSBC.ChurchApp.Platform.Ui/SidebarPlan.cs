using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Platform.Ui;

internal sealed record SidebarArea(NavArea Area, IReadOnlyList<NavEntry> Entries, bool Open, int Waiting);

internal sealed record SidebarPlan(IReadOnlyList<NavEntry> Top, IReadOnlyList<NavEntry> Pinned, IReadOnlyList<SidebarArea> Areas, IReadOnlyList<NavEntry> Foot)
{
    private const int FewEnoughToShowOpen = 8;

    public static SidebarPlan Of(IReadOnlyList<NavEntry> permitted, IReadOnlyList<string> pins, IReadOnlyDictionary<string, bool> chosen, NavArea? here, IReadOnlyDictionary<string, int>? counts)
    {
        List<NavEntry> inAreas = permitted.Where(Pinnable).ToList();
        bool openByDefault = inAreas.Count <= FewEnoughToShowOpen;
        List<NavEntry> pinned = pins.Select(href => inAreas.FirstOrDefault(entry => entry.Href == href)).OfType<NavEntry>().ToList();
        List<SidebarArea> areas = inAreas
            .GroupBy(entry => entry.Area!)
            .OrderBy(area => area.Key.Order)
            .Select(area => new SidebarArea(area.Key, area.ToList(), area.Key == here || chosen.GetValueOrDefault(area.Key.Title, openByDefault),
                area.Sum(entry => counts?.GetValueOrDefault(entry.Href) ?? 0)))
            .ToList();
        return new SidebarPlan(permitted.Where(entry => entry.Area is null).ToList(), pinned, areas, permitted.Where(entry => entry.Area is { AtFoot: true }).ToList());
    }

    public static IReadOnlyList<NavEntry> InOrder(IEnumerable<NavEntry> entries) => entries
        .OrderBy(entry => entry.Area?.Order ?? 0)
        .ThenBy(entry => entry.Area is null ? "" : entry.Title, StringComparer.CurrentCulture)
        .ToList();

    public static bool Pinnable(NavEntry entry) => entry.Area is { AtFoot: false };
}
