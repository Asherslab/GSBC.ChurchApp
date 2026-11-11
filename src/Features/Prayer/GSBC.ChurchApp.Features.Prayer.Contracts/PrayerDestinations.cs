using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerDestinations
{
    public static readonly DestinationPage Prayer = new("/prayer", "Prayer", "prayer", DestinationAreas.Pages, "Prayer", Audience.Everyone, Rank: 45);

    public static readonly DestinationPage Ask = new("/prayer/ask", "Ask for prayer", "prayer", DestinationAreas.Pages, "Prayer → Ask for prayer", Audience.Everyone, Rank: 46);

    public static readonly DestinationPage ToApprove = new("/prayer/approve", "Prayer requests to approve", "prayer", DestinationAreas.Pages,
        "Prayer → To approve", Audience.Holding(PrayerPermissions.WallNode), Rank: 47);

    public static readonly DestinationArea Area = new("prayer", "Prayer", "prayer", 45);

    public static readonly DestinationKind StaffRequest = new("prayer.request", Area, "Prayer request", "prayer", "prayer/requests");

    public static readonly DestinationKind YourRequest = new("prayer.yours", Area, "Your prayer request", "prayer", "prayer/yours");

    public static string Approve => ToApprove.Path;

    public static string Request(string id) => StaffRequest.PathOf(id);

    public static string Yours(Guid id) => YourRequest.PathOf(id.ToString());

    public static readonly DestinationSet All = new([Prayer, Ask, ToApprove], [StaffRequest, YourRequest]);
}
