using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsClientExtensions
{
    public static readonly IReadOnlyList<string> Feeds = [EventsFeeds.Calendars, EventsFeeds.Public];
    public static readonly IReadOnlyList<string> PrivateFeeds = [EventsFeeds.PrivateCalendars, EventsFeeds.Private, EventsFeeds.Contacts];

    public static IServiceCollection AddEventsClient(this IServiceCollection services) =>
        services.AddDestinations(EventsDestinations.All);
}
