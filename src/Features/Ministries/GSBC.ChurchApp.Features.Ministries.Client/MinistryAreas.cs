using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryAreas
{
    private static readonly IReadOnlyDictionary<string, string> PlaceOf = new Dictionary<string, string>
    {
        [MinistryParts.News] = MinistriesPlaces.News, [MinistryParts.Calendar] = MinistriesPlaces.Calendar, [MinistryParts.Files] = MinistriesPlaces.Files,
        [MinistryParts.Rosters] = MinistriesPlaces.Roster, [MinistryParts.Chats] = MinistriesPlaces.Chats,
    };

    public static IReadOnlyList<(string Key, string Label, int? Count)> For(MinistryPage page)
    {
        YourPlace you = page.You;
        List<(string, string, int?)> areas = [(MinistriesPlaces.About, "About", null)];
        foreach (string part in MinistryParts.Ordered(page.Ministry.PartsOrder))
            if (MinistryParts.HasTab(page, part))
                areas.Add((PlaceOf[part], MinistryParts.Of(part).Name, null));
        if (you.MayManagePeople)
            areas.Add((MinistriesPlaces.People, "People", you.Waiting));
        if (you.MayEditSettings)
            areas.Add((MinistriesPlaces.Settings, "Settings", null));
        return areas;
    }

    public static bool Uses(MinistryPage page, string part) => MinistryParts.IsOn(page.Ministry.PartsOff, part);

    public static string Known(MinistryPage page, string area)
    {
        string wanted = area == MinistriesPlaces.Sundays ? MinistriesPlaces.Calendar : area;
        return For(page).Any(each => each.Key == wanted) ? wanted : MinistriesPlaces.About;
    }

    public static IReadOnlyList<TabOption<string>> Tabs(MinistryPage page) =>
        For(page).Select(each => new TabOption<string>(each.Key, each.Label, each.Count)).ToList();
}
