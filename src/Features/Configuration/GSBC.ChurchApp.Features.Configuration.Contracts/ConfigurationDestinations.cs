using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ConfigurationDestinations
{
    public static readonly DestinationPage Previews = new("/previews", "Previews", "eye", DestinationAreas.Pages, "More → Previews: drafts shared with you", Audience.SignedIn);

    public static readonly DestinationSet All = DestinationSet.Of(Previews);
}
