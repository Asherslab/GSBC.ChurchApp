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
        if (Uses(page, MinistryParts.News) && (page.Posts.Count > 0 || posts))
            areas.Add((MinistriesPlaces.News, MinistryParts.Of(MinistryParts.News).Name, null));
        if (Uses(page, MinistryParts.Calendar) && (page.Calendars.Count > 0 || you.MayEditSettings))
            areas.Add((MinistriesPlaces.Calendar, MinistryParts.Of(MinistryParts.Calendar).Name, null));
        if (Uses(page, MinistryParts.Files) && (page.Files.Count > 0 || posts))
            areas.Add((MinistriesPlaces.Files, MinistryParts.Of(MinistryParts.Files).Name, null));
        if (you.MayManagePeople)
            areas.Add((MinistriesPlaces.People, "People", you.Waiting));
        if (you.MayEditSettings)
            areas.Add((MinistriesPlaces.Settings, "Settings", null));
        return areas;
    }

    public static bool Uses(MinistryPage page, string part) => MinistryParts.IsOn(page.Ministry.PartsOff, part);

    public static string Known(MinistryPage page, string area) =>
        For(page).Any(each => each.Key == area) ? area : MinistriesPlaces.About;

    public static IReadOnlyList<TabOption<string>> Tabs(MinistryPage page) =>
        For(page).Select(each => new TabOption<string>(each.Key, each.Label, each.Count)).ToList();
}
