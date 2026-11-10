using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventsPlaces
{
    public const string Week = "/week";
    public const string Month = "/week?view=month";
    public const string Calendars = "/week/calendars";
    public const string NewEvent = "/event/new";

    public static string EventPath(string occurrenceId) => EventsDestinations.Event.PathOf(occurrenceId);

    public static string Open(EventOccurrence occurrence) => occurrence.OpensAt.Length > 0 ? occurrence.OpensAt : EventPath(occurrence.Id);

    public static string CalendarPath(CalendarInfo calendar) => EventsDestinations.Calendar.PathOf(calendar.Slug.Length > 0 ? calendar.Slug : calendar.Id);

    public static string NewEventIn(string calendarId) => $"{NewEvent}?calendar={Uri.EscapeDataString(calendarId)}";

    public static string OnceIn(string calendarId) => $"{NewEventIn(calendarId)}&how=once";

    public static string CalendarById(string calendarId) => EventsDestinations.Calendar.PathOf(calendarId);

    public static string NewRegularIn(string calendarId) => $"{CalendarById(calendarId)}/regular/new";

    public static string RegularPath(string calendarId, string regularEventId) => $"{CalendarById(calendarId)}/regular/{Uri.EscapeDataString(regularEventId)}";

    public static string RegularDatePath(string calendarId, string regularEventId, string date) => $"{RegularPath(calendarId, regularEventId)}/{date}";
}
