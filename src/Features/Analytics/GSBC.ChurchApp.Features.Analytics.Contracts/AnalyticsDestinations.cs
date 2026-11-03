using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static class AnalyticsDestinations
{
    public static readonly DestinationPage HelpImprove = new("/help-improve", "Help improve the app", "heart", DestinationAreas.Pages, "More → Help improve the app", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(HelpImprove);
}
