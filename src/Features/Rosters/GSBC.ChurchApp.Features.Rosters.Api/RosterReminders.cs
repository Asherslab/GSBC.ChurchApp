using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterReminders(RostersDbContext db, RosterMessenger messenger, IMinistryFacts ministries, TimeProvider time)
{
    public async Task SendAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime horizon = now.AddDays(RosterRules.MostDaysBefore);
        Dictionary<Guid, MinistryFacts> live = (await ministries.UsingAsync(RosterParts.Key, cancellationToken))
            .Where(ministry => !ministry.Archived).ToDictionary(ministry => ministry.Id);
        if (live.Count == 0)
            return;

        List<Guid> ids = live.Keys.ToList();
        Dictionary<Guid, RosterSettings> settings = (await db.Settings.AsNoTracking().Where(each => ids.Contains(each.MinistryId)).ToListAsync(cancellationToken))
            .ToDictionary(each => each.MinistryId, RosterRules.SettingsOf);
        var waiting = await db.Assignments
            .Where(assignment => ids.Contains(assignment.MinistryId) && assignment.PersonId != null && (assignment.RemindedAt == null || assignment.NudgedAt == null))
            .Join(db.Dates.AsNoTracking().Where(date => date.StartsAt > now && date.StartsAt <= horizon && !date.NotOn && !date.Gone),
                assignment => assignment.DateId, date => date.Id, (assignment, date) => new { assignment, date })
            .ToListAsync(cancellationToken);

        foreach (var each in waiting)
        {
            AssignmentRecord assignment = each.assignment;
            DateRecord date = each.date;
            RosterSettings rules = settings.GetValueOrDefault(assignment.MinistryId) ?? RosterRules.SettingsOf(null);
            MinistryFacts ministry = live[assignment.MinistryId];
            Guid person = assignment.PersonId!.Value;
            if (RosterRules.ReminderDue(assignment.State, date.StartsAt, now, rules.RemindDaysBefore, assignment.RemindedAt))
            {
                assignment.RemindedAt = now;
                await db.SaveChangesAsync(cancellationToken);
                await messenger.TellAsync(person, RostersNotifications.Reminder, $"You're on {RosterText.Day(date.StartsAt)}",
                    $"{RosterText.Line(date, assignment.PositionName)}, for {ministry.Name}.", ministry.Id, $"rosters/reminder/{assignment.Id:N}", null, cancellationToken);
            }
            else if (rules.Replies && RosterRules.NudgeDue(assignment.State, assignment.AssignedAt, date.StartsAt, now, rules.NudgeAfterDays, assignment.NudgedAt))
            {
                assignment.NudgedAt = now;
                await db.SaveChangesAsync(cancellationToken);
                await messenger.TellAsync(person, RostersNotifications.Nudge, $"Can you do {RosterText.Day(date.StartsAt)}?",
                    $"{RosterText.Line(date, assignment.PositionName)}, for {ministry.Name}. Say whether you can do it.", ministry.Id, $"rosters/on/{assignment.Id:N}",
                    null, cancellationToken, "Answer");
            }
        }
    }
}
