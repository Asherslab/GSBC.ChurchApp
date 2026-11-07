using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsClientExtensions
{
    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Calendars, EventsFeeds.Public];
    public static readonly IReadOnlyList<string> PrivateFeeds = [EventsFeeds.PrivateCalendars, EventsFeeds.Private, EventsFeeds.Contacts];

    public static IServiceCollection AddEventsOnPhone(this IServiceCollection services) =>
        services
            .AddEventsClient()
            .AddSingleton<YourCalendars>()
            .AddSingleton<IYourCalendarLink, PhoneCalendarLink>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<YourCalendars>());

    public static IServiceCollection AddEventsClient(this IServiceCollection services) =>
        services
            .AddDestinations(EventsDestinations.All)
            .AddSingleton<CalendarDiary>()
            .AddSingleton<CalendarEditing>()
            .AddSingleton<CalendarSubscriptions>()
            .AddSingleton<WideningWords>()
            .AddHubTile<ComingUpTile>(EventsTiles.Week);
}
