using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryAreas
{
    public static IReadOnlyList<(string Key, string Label, int? Count)> For(MinistryPage page)
    {
        YourPlace you = page.You;
        bool posts = you.MayPostFor.Count > 0;
        bool lifeGroup = page.Ministry.Kind == MinistryKinds.LifeGroup;
        List<(string, string, int?)> areas = [(MinistriesPlaces.About, "About", null)];
        if (lifeGroup && page.Round is not null)
            areas.Add((MinistriesPlaces.Sundays, "Sundays", null));
        if (page.Posts.Count > 0 || posts)
            areas.Add((MinistriesPlaces.News, "News", null));
        if (!lifeGroup && (page.Calendars.Count > 0 || you.MayEditSettings))
            areas.Add((MinistriesPlaces.Calendar, "Calendar", null));
        if (page.Files.Count > 0 || posts)
            areas.Add((MinistriesPlaces.Files, "Files", null));
        if (you.MayManagePeople)
            areas.Add((MinistriesPlaces.People, "People", you.Waiting));
        if (you.MayEditSettings)
            areas.Add((MinistriesPlaces.Settings, "Settings", null));
        return areas;
    }

    public static string Known(MinistryPage page, string area) =>
        For(page).Any(each => each.Key == area) ? area : MinistriesPlaces.About;

    public static IReadOnlyList<TabOption<string>> Tabs(MinistryPage page) =>
        For(page).Select(each => new TabOption<string>(each.Key, each.Label, each.Count)).ToList();
}
