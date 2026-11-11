using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class DiaryEventChoices(CalendarDiary diary) : IEventChoices
{
    public IReadOnlyList<string> Feeds => CalendarDiary.Feeds;

    public async Task<IReadOnlyList<EventChoiceLine>> UpcomingAsync(DateTime fromUtc, DateTime toUtc)
    {
        Diary loaded = await diary.LoadAsync();
        return loaded.Occurrences
            .Where(occurrence => !occurrence.NotOn && occurrence.StartsAt >= fromUtc && occurrence.StartsAt <= toUtc)
            .Select(occurrence => Line(loaded, occurrence))
            .ToList();
    }

    private static EventChoiceLine Line(Diary diary, EventOccurrence occurrence)
    {
        CalendarInfo? calendar = diary.Calendar(occurrence.CalendarId);
        return new EventChoiceLine(occurrence.Id, occurrence.Fields.Title, calendar?.Name ?? "", calendar?.MinistryId, occurrence.StartsAt, occurrence.EndsAt, occurrence.AllDay);
    }
}
