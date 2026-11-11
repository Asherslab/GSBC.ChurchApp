using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Platform.Api.Calendars;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterRefresh(RostersDbContext db, IRosterableEvents events, TimeProvider time)
{
    public async Task RefreshEventsAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<DateRecord> coming = await db.Dates.Where(date => date.EndsAt >= now).ToListAsync(cancellationToken);
        if (coming.Count == 0)
            return;

        IReadOnlyDictionary<string, RosterableEvent> found = await events.FindAsync(coming.Select(date => date.EventKey).ToList(), cancellationToken);
        foreach (DateRecord date in coming)
        {
            if (found.GetValueOrDefault(date.EventKey) is not { } current)
            {
                date.Gone = true;
                continue;
            }
            (date.Title, date.CalendarName, date.StartsAt, date.EndsAt, date.AllDay, date.NotOn, date.Gone) =
                (current.Title, current.CalendarName, current.StartsAt, current.EndsAt, current.AllDay, current.NotOn, false);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
