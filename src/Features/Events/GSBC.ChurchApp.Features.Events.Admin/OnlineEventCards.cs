using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class OnlineEventCards(CalendarEditing editing, TimeProvider time) : IEventCards
{
    private static readonly TimeSpan KeptFor = TimeSpan.FromMinutes(1);

    private Task<Diary>? _loading;
    private DateTimeOffset _loadedAt;

    public IReadOnlyList<string> Feeds => [];

    public Task<Diary> DiaryAsync()
    {
        DateTimeOffset now = time.GetUtcNow();
        if (_loading is null || _loading.IsFaulted || now - _loadedAt > KeptFor)
            (_loading, _loadedAt) = (LoadAsync(), now);
        return _loading;
    }

    private async Task<Diary> LoadAsync()
    {
        List<CalendarInfo> calendars = (await editing.DeskAsync()).Calendars.Select(calendar => calendar.Settings.Calendar).ToList();
        DateOnly today = ChurchTime.Today(time);
        Agenda agenda = await editing.AgendaAsync(calendars.Select(calendar => calendar.Id), today.AddDays(-EventsFeeds.DaysBack), today.AddMonths(EventsFeeds.MonthsAhead));
        return new Diary(calendars, agenda.Occurrences.OrderBy(occurrence => occurrence.StartsAt).ToList(), new Dictionary<string, List<EventContact>>());
    }
}
