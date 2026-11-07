using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsPlaces
{
    public const string Week = "/week";
    public const string Month = "/week?view=month";
    public const string Calendars = "/week/calendars";
    public const string NewEvent = "/event/new";

    public static string EventPath(string occurrenceId) => EventsDestinations.Event.PathOf(occurrenceId);

    public static string CalendarPath(CalendarInfo calendar) => EventsDestinations.Calendar.PathOf(calendar.Slug.Length > 0 ? calendar.Slug : calendar.Id);

    public static string NewEventIn(string calendarId) => $"{NewEvent}?calendar={Uri.EscapeDataString(calendarId)}";
}
