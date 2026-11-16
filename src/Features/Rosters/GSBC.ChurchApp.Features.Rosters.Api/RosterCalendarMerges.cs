using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Platform.Api.Calendars;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterCalendarMerges(RostersDbContext db, RosterPublisher publisher) : ICalendarMerges
{
    public async ValueTask MergedAsync(Guid fromCalendarId, Guid intoCalendarId, CancellationToken cancellationToken)
    {
        List<SettingsRecord> drawing = await db.Settings.Where(settings => settings.Calendars.Contains(fromCalendarId)).ToListAsync(cancellationToken);
        foreach (SettingsRecord settings in drawing)
            settings.Calendars = settings.Calendars.Select(id => id == fromCalendarId ? intoCalendarId : id).Distinct().ToList();
        await db.SaveChangesAsync(cancellationToken);
        foreach (SettingsRecord settings in drawing)
            await publisher.PublishAsync(settings.MinistryId, cancellationToken);
    }
}
