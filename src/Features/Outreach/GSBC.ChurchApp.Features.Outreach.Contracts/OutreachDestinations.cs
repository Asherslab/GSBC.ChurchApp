using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachDestinations
{
    public static readonly DestinationPage Outreach = new("/" + OutreachLinks.Home, "Outreach", AppIcons.Outreach, DestinationAreas.Pages, "Outreach",
        Audience.Everyone, Rank: 60);

    public static readonly DestinationPage WalkFromYourDoor = new("/" + OutreachLinks.PlanWalk, "Walk from your door", AppIcons.Outreach, DestinationAreas.Pages,
        "Outreach → Walk from your door", Audience.Everyone, Rank: 61);

    public static readonly DestinationPage MyOutreach = new("/" + OutreachLinks.Mine, "My outreach", AppIcons.Outreach, DestinationAreas.Pages,
        "Outreach → My outreach", Audience.Everyone, Rank: 62);

    public static readonly DestinationPage OfflineMaps = new("/" + OutreachLinks.OfflineMaps, "Offline maps", AppIcons.Outreach, DestinationAreas.Pages,
        "Outreach → Offline maps", Audience.Everyone, Rank: 63);

    public static readonly DestinationArea Area = new("outreach", "Outreach", AppIcons.Outreach, 60);

    public static readonly DestinationKind Day = new("outreach.day", Area, "Outreach day", AppIcons.Outreach, "outreach/days", Feeds: [OutreachFeeds.Days]);

    public static readonly DestinationSet All = new([Outreach, WalkFromYourDoor, MyOutreach, OfflineMaps], [Day]);
}
