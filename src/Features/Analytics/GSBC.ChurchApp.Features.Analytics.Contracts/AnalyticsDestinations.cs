using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static class AnalyticsDestinations
{
    public static readonly DestinationPage HelpImprove = new("/help-improve", "Help improve the app", AppIcons.HelpImproveTheApp, DestinationAreas.Pages, "More → Help improve the app", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(HelpImprove);
}
