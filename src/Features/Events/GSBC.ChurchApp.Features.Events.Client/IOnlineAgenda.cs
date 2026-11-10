using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IOnlineAgenda
{
    Task<IReadOnlyList<string>> ShownOnNewPhonesAsync();

    Task<IReadOnlyList<EventOccurrence>> AgendaAsync(IEnumerable<string> calendarIds, DateOnly from, DateOnly to);
}
