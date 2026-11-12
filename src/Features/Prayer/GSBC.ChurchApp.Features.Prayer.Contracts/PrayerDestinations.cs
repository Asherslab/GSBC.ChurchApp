using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerDestinations
{
    public static readonly DestinationPage Prayer = new("/prayer", "Prayer", AppIcons.Prayer, DestinationAreas.Pages, "Prayer", Audience.Everyone, Rank: 45);

    public static readonly DestinationPage Ask = new("/prayer/ask", "Ask for prayer", AppIcons.Prayer, DestinationAreas.Pages, "Prayer → Ask for prayer", Audience.Everyone, Rank: 46);

    public const string Approve = "/prayer/approve";

    public static string Request(string id) => $"/prayer/requests/{id}";

    public static readonly DestinationSet All = DestinationSet.Of([Prayer, Ask]);
}
