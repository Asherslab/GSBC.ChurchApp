using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Tabs;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.App;

public static class SermonsAppExtensions
{
    public const string Route = "sermons";

    public static IServiceCollection AddSermonsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Sermons", typeof(SermonsAppExtensions).Assembly,
            [new NavEntry("Sermons", Route, "sermons",
                AlsoUnder: [SermonsDestinations.Sermon.PathPrefix, SermonsDestinations.Series.PathPrefix, SermonsDestinations.Speaker.PathPrefix])],
            SermonCatalogue.Feeds,
            [.. Listening.Feeds, SermonsFeeds.Follows]));
        return services.AddSermonsOnPhone().AddStoragePart<DownloadsStoragePart>().AddSingleton<ILiveTabIcon, SermonsTabIcon>();
    }
}
