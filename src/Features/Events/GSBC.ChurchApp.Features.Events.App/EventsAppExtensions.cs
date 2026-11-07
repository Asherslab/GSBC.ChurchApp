using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.App;

public static class EventsAppExtensions
{
    public static IServiceCollection AddEventsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Events", typeof(EventsAppExtensions).Assembly,
            [new NavEntry("Calendar", EventsDestinations.Week.Route, "calendar",
                AlsoUnder: [EventsDestinations.Event.PathPrefix, EventsDestinations.Calendar.PathPrefix])],
            EventsClientExtensions.Feeds,
            EventsClientExtensions.PrivateFeeds));
        return services.AddEventsClient();
    }
}
