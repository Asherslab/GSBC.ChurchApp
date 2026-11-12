using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ComingSoonDestinations
{
    public static readonly DestinationPage Sunday = new("/sunday", "Sunday bulletin", AppIcons.SundayBulletin, DestinationAreas.Pages, "Coming soon", Audience.Everyone);
    public static readonly DestinationPage Outreach = new("/outreach", "Outreach", AppIcons.Outreach, DestinationAreas.Pages, "Coming soon", Audience.Everyone);

    public static readonly IReadOnlyList<DestinationPage> Pages = [Sunday, Outreach];

    public static readonly DestinationSet All = DestinationSet.Of(Pages);
}
