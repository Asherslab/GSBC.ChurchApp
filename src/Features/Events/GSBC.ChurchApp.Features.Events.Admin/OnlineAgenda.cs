using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class OnlineAgenda(CalendarEditing editing) : IOnlineAgenda
{
    public async Task<IReadOnlyList<string>> ShownOnNewPhonesAsync() =>
        (await editing.DeskAsync()).Calendars.Where(calendar => calendar.Settings.Calendar.ShownOnNewPhones).Select(calendar => calendar.Settings.Calendar.Id).ToList();

    public async Task<IReadOnlyList<EventOccurrence>> AgendaAsync(IEnumerable<string> calendarIds, DateOnly from, DateOnly to) =>
        (await editing.AgendaAsync(calendarIds, from, to)).Occurrences;
}
