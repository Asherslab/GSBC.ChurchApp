using System.Globalization;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class DetailsReminders(EventsDbContext db, INotifications notifications, IPermissionHolders holders, TimeProvider time)
{
    public static readonly IReadOnlyList<int> DefaultDaysBefore = [7, 2];

    public async Task RemindAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateOnly today = ChurchTime.Today(time);
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        Dictionary<Guid, RegularEventRecord> regulars = book.RegularEvents.ToDictionary(regular => regular.Id);
        Dictionary<string, EventOccurrence> upcoming = book
            .Occurrences(today, today.AddDays(EventWriter.MostRemindDays), item => item is RegularEventRecord { Needs.Count: > 0 } && EventsBook.IsShown(item, now))
            .ToDictionary(occurrence => occurrence.Id);
        List<DetailsReminderRecord> sent = await db.DetailsReminders.Where(reminder => reminder.SettledAt == null || reminder.Date >= today).ToListAsync(cancellationToken);

        await SettleAsync(sent.Where(reminder => reminder.SettledAt is null && !StillMissing(upcoming.GetValueOrDefault(reminder.OccurrenceId))), now, cancellationToken);

        ILookup<string, int> already = sent.ToLookup(reminder => reminder.OccurrenceId, reminder => reminder.DaysBefore);
        foreach (EventOccurrence occurrence in upcoming.Values.Where(StillMissing).OrderBy(occurrence => occurrence.StartsAt))
        {
            RegularEventRecord regular = regulars[Guid.Parse(occurrence.ItemId)];
            DateOnly date = EventDates.Parse(occurrence.Date)!.Value;
            int daysToGo = date.DayNumber - today.DayNumber;
            List<int> due = (regular.RemindDaysBefore.Count > 0 ? regular.RemindDaysBefore : DefaultDaysBefore)
                .Where(days => daysToGo <= days && !already[occurrence.Id].Contains(days)).ToList();
            if (due.Count == 0)
                continue;

            await notifications.RaiseAsync(await ReminderAsync(regular, occurrence, date, cancellationToken), cancellationToken);
            db.DetailsReminders.AddRange(due.Select(days => new DetailsReminderRecord
            {
                OccurrenceId = occurrence.Id, DaysBefore = days, RegularEventId = regular.Id, Date = date, SentAt = now,
            }));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static string Subject(string occurrenceId) => $"event/{occurrenceId}/details";

    private static bool StillMissing(EventOccurrence? occurrence) => occurrence is { NotOn: false, Missing.Count: > 0 };

    private async Task SettleAsync(IEnumerable<DetailsReminderRecord> settled, DateTime now, CancellationToken cancellationToken)
    {
        foreach (IGrouping<string, DetailsReminderRecord> occurrence in settled.GroupBy(reminder => reminder.OccurrenceId).ToList())
        {
            await notifications.SettleAsync(Subject(occurrence.Key), cancellationToken);
            foreach (DetailsReminderRecord reminder in occurrence)
                reminder.SettledAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Notification> ReminderAsync(RegularEventRecord regular, EventOccurrence occurrence, DateOnly date, CancellationToken cancellationToken)
    {
        PermissionHolders organisers = await holders.HoldersAsync(EventsPermissions.Organise, regular.Id.ToString(), cancellationToken);
        NotificationAudience audience = organisers.People.Count > 0
            ? NotificationAudience.Holding(EventsPermissions.Organise, regular.Id.ToString())
            : NotificationAudience.Holding(EventsPermissions.Edit, regular.CalendarId.ToString());
        string when = date.ToString("dddd d MMMM", CultureInfo.GetCultureInfo("en-AU"));
        return new Notification(EventsNotifications.DetailsNeeded, audience,
            $"{occurrence.Fields.Title} on {when} needs {Words(occurrence.Missing)}",
            "Add it in the calendar so people know before they come.",
            Link: $"/event/{occurrence.Id}", Subject: Subject(occurrence.Id), ScopeId: regular.CalendarId.ToString(), Action: "Add details");
    }

    private static string Words(IReadOnlyList<string> missing)
    {
        List<string> words = missing.Select(need => need switch
        {
            EventFieldKeys.Place => "a place",
            EventFieldKeys.Contacts => "a contact",
            EventFieldKeys.Description => "a description",
            EventFieldKeys.Picture => "a picture",
            EventFieldKeys.Register => "a register link",
            _ => "details",
        }).Distinct().ToList();
        return words.Count == 1 ? words[0] : $"{string.Join(", ", words[..^1])} and {words[^1]}";
    }
}
