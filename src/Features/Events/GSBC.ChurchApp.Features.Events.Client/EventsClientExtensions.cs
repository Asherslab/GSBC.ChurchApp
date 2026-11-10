using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsClientExtensions
{
    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Calendars, EventsFeeds.Public, EventsFeeds.Digest];
    public static readonly IReadOnlyList<string> PrivateFeeds = [EventsFeeds.PrivateCalendars, EventsFeeds.Private, EventsFeeds.Contacts, EventsFeeds.PrivateDigest];

    public static IServiceCollection AddEventsOnPhone(this IServiceCollection services) =>
        services
            .AddEventsClient()
            .AddSingleton<YourCalendars>()
            .AddSingleton<IYourCalendarLink, PhoneCalendarLink>()
            .AddSingleton<IEventChoices, DiaryEventChoices>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<YourCalendars>());

    public static IServiceCollection AddEventsClient(this IServiceCollection services) =>
        services
            .AddDestinations(EventsDestinations.All)
            .AddSingleton<CalendarDiary>()
            .AddSingleton<DigestReader>()
            .AddSingleton<CalendarEditing>()
            .AddSingleton<CalendarSubscriptions>()
            .AddSingleton<WideningWords>()
            .AddSingleton(new CalendarSetView(CalendarSetList.For))
            .AddHubTile<ComingUpTile>(EventsTiles.Week);
}
