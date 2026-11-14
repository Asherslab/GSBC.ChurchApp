using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Content;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsClientExtensions
{
    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Calendars, EventsFeeds.Public, EventsFeeds.Digest];
    public static readonly IReadOnlyList<string> PrivateFeeds = [EventsFeeds.PrivateCalendars, EventsFeeds.Private, EventsFeeds.Contacts, EventsFeeds.PrivateDigest, EventsFeeds.Editing, EventsFeeds.Mine];

    public static IServiceCollection AddEventsOnPhone(this IServiceCollection services) =>
        services
            .AddEventsClient()
            .AddSingleton<YourCalendars>()
            .AddSingleton<QueuedEventEdits>()
            .AddSingleton<IEventEditing, StoredEventEditing>()
            .AddSingleton<IYourCalendarLink, PhoneCalendarLink>()
            .AddSingleton<IEventChoices, DiaryEventChoices>()
            .AddSingleton<IEventCards, DiaryEventCards>()
            .AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<YourCalendars>());

    public static IServiceCollection AddEventsClient(this IServiceCollection services) =>
        services
            .AddDestinations(EventsDestinations.All)
            .AddSingleton<CalendarDiary>()
            .AddSingleton<DigestReader>()
            .AddSingleton<CalendarSubscriptions>()
            .AddSingleton<WideningWords>()
            .AddSingleton(new CalendarSetView(CalendarSetList.For))
            .AddHubTile<ComingUpTile>(EventsTiles.Week)
            .AddAppCardView<EventCard>(AppCardKind.Event)
            .AddAppCardSource<EventCardChoices>();
}
