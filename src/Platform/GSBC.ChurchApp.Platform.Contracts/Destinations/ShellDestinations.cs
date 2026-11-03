using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public static class ShellDestinations
{
    public static readonly DestinationPage Home = new("/", "Home", "home", DestinationAreas.Pages, "The first tab", Audience.Everyone, Rank: 0);
    public static readonly DestinationPage More = new("/more", "More", "more", DestinationAreas.Pages, "The last tab", Audience.Everyone, Rank: 30);
    public static readonly DestinationPage WhatsNew = new("/whats-new", "What's new", "sparkles", DestinationAreas.Pages, "More → What's new", Audience.Everyone, Rank: 20);
    public static readonly DestinationPage Appearance = new("/appearance", "Appearance", "sun", DestinationAreas.Pages, "More → Appearance", Audience.Everyone);
    public static readonly DestinationPage Storage = new("/storage", "Storage", "database", DestinationAreas.Pages, "More → Storage", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Home, More, WhatsNew, Appearance, Storage);
}
