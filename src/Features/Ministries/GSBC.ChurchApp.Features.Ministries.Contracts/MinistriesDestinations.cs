using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesDestinations
{
    public static readonly DestinationArea Area = new("ministries", "Ministries", "users", 40, Browsing: "every ministry");

    public static readonly DestinationPage Directory = new("/ministries", "Ministries", AppIcons.Ministries, DestinationAreas.Pages, "Ministries", Audience.Everyone, Rank: 40);

    public static readonly DestinationPage LifeGroups = new("/life-groups", "Life Groups", AppIcons.LifeGroups, DestinationAreas.Pages, "Ministries", Audience.Everyone, Rank: 41);

    public static readonly DestinationKind Ministry = new("ministries.ministry", Area, "Ministry", "users", "ministries", Feeds: [MinistriesFeeds.Ministries]);

    public static readonly DestinationSet All = new([Directory, LifeGroups], [Ministry]);
}
