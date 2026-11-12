using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsDestinations
{
    public static readonly DestinationArea Area = new("sermons", "Sermons", "sermons", 20, Browsing: "latest");

    public static readonly DestinationPage Listen = new("/sermons", "Sermons", AppIcons.Sermons, DestinationAreas.Pages, "The Listen tab", Audience.Everyone, Rank: 20);

    public static readonly DestinationKind Sermon = new("sermons.sermon", Area, "Sermon", "sermons", "sermon", Feeds: [SermonsFeeds.Catalogue]);
    public static readonly DestinationKind Series = new("sermons.series", Area, "Series", "layers", "series", Feeds: [SermonsFeeds.Series]);
    public static readonly DestinationKind Speaker = new("sermons.speaker", Area, "Speaker", "user", "speaker", Feeds: [SermonsFeeds.Speakers]);

    public static readonly DestinationSet All = new([Listen], [Sermon, Series, Speaker]);
}
