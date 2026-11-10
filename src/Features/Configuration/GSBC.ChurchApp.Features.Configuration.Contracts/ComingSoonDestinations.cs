using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ComingSoonDestinations
{
    public static readonly DestinationPage Sunday = new("/sunday", "Sunday bulletin", "bulletin", DestinationAreas.Pages, "Coming soon", Audience.Everyone);
    public static readonly DestinationPage Outreach = new("/outreach", "Outreach", "footprints", DestinationAreas.Pages, "Coming soon", Audience.Everyone);

    public static readonly IReadOnlyList<DestinationPage> Pages = [Sunday, Outreach];

    public static readonly DestinationSet All = DestinationSet.Of(Pages);
}
