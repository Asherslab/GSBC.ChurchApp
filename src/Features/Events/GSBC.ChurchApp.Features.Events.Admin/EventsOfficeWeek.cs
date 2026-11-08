using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Admin;

internal sealed class EventsOfficeWeek(CalendarEditing editing, IAccess access, TimeProvider clock) : IOfficeWeekSource
{
    private const int DueWithinDays = 7;

    public IReadOnlyCollection<string> Feeds { get; } = [EventsFeeds.Admin];

    public async Task<OfficeWeek> LoadAsync(DateTime from, DateTime until)
    {
        if (!access.CanSomewhere(EventsPermissions.Edit))
            return OfficeWeek.Nothing;

        DateTime now = clock.GetUtcNow().UtcDateTime;
        Task<NeedsDetailsList> needs = editing.Editing.NeedsDetailsAsync(new Done()).AsTask();
        Task<AskedList> asked = editing.Editing.AskedToPublishAsync(new Done()).AsTask();
        Task<Agenda> agenda = editing.AgendaAsync([], ChurchTime.DayOf(from), ChurchTime.DayOf(until)).AsTask();
        await Task.WhenAll(needs, asked, agenda);

        List<OfficeTask> tasks =
        [
            .. needs.Result.Items.Where(item => item.DaysToGo <= DueWithinDays).Select(item => new OfficeTask(item.Occurrence.Fields.Title,
                $"{EventWords.NeedsLine(item.Occurrence.Missing)} · {EventWords.LongDate(EventWords.DayOf(item.Occurrence))}",
                CalendarWorkbenchLinks.Event(item.Occurrence.Id, CalendarWorkbenchLinks.NeedsDetails), "Fill it in", now)),
            .. asked.Result.Items.Select(item => new OfficeTask(item.Title, "Waiting for you to publish it",
                CalendarWorkbenchLinks.AskedToPublish, "Look", item.AskedAt)),
        ];
        List<OfficeSpan> spans = agenda.Result.Occurrences
            .Where(occurrence => occurrence.Kind != OccurrenceKind.Regular && !occurrence.NotOn)
            .Select(occurrence => new OfficeSpan(occurrence.Fields.Title,
                occurrence.StartsAt, occurrence.EndsAt, CalendarWorkbenchLinks.Event(occurrence.Id), OfficeSpanKind.Everyone))
            .ToList();
        return new OfficeWeek(tasks, spans);
    }
}

public static class CalendarWorkbenchLinks
{
    public const string Page = "calendar";
    public const string NeedsDetails = "needs";
    public const string Asked = "asked";
    public const string Breaks = "breaks";
    public const string AskedToPublish = $"{Page}?show={Asked}";

    public static string Showing(string? show) => show is null ? Page : $"{Page}?show={show}";

    public static string Calendar(string slug) => $"{Page}?calendar={Uri.EscapeDataString(slug)}";

    public static string Event(string occurrenceId, string? show = null) =>
        show is null ? $"{Page}?event={Uri.EscapeDataString(occurrenceId)}" : $"{Page}?show={show}&event={Uri.EscapeDataString(occurrenceId)}";
}
