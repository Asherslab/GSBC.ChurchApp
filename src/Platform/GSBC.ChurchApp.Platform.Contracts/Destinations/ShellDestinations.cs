using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public static class ShellDestinations
{
    public static readonly DestinationPage Home = new("/", "Home", AppIcons.Home, DestinationAreas.Pages, "The first tab", Audience.Everyone, Rank: 0);
    public static readonly DestinationPage More = new("/more", "More", AppIcons.More, DestinationAreas.Pages, "The last tab", Audience.Everyone, Rank: 30);
    public static readonly DestinationPage WhatsNew = new("/whats-new", "What's new", AppIcons.WhatsNew, DestinationAreas.Pages, "More → What's new", Audience.Everyone, Rank: 20);
    public static readonly DestinationPage Appearance = new("/appearance", "Appearance", AppIcons.Appearance, DestinationAreas.Pages, "More → Appearance", Audience.Everyone);
    public static readonly DestinationPage Storage = new("/storage", "Storage", AppIcons.Storage, DestinationAreas.Pages, "More → Storage", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Home, More, WhatsNew, Appearance, Storage);
}
