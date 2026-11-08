using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsDestinations
{
    public static readonly DestinationArea Area = new("events", "The church calendar", "calendar", 25, Browsing: "coming up");

    public static readonly DestinationPage Week = new("/week", "This week", "calendar", DestinationAreas.Pages, "The church calendar", Audience.Everyone, Rank: 25);

    public static readonly DestinationPage WhatsNext = new("/whats-next", "What's next", "megaphone", DestinationAreas.Pages, "The church calendar", Audience.Everyone, Rank: 26);

    public static readonly DestinationKind Event = new("events.event", Area, "Event", "calendar", "event", Feeds: [EventsFeeds.Public, EventsFeeds.Private]);
    public static readonly DestinationKind Calendar = new("events.calendar", Area, "Calendar", "layers", "calendar", Feeds: [EventsFeeds.Calendars, EventsFeeds.PrivateCalendars]);

    public static readonly DestinationSet All = new([Week, WhatsNext], [Event, Calendar]);
}
