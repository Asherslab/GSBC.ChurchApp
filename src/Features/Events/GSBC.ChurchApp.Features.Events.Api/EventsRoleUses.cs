using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsRoleUses(EventsDbContext db) : IRoleUses
{
    public async ValueTask<IReadOnlyList<string>> UsesAsync(Guid roleId, CancellationToken cancellationToken)
    {
        string id = roleId.ToString("D");
        return await db.Calendars.AsNoTracking()
            .Where(calendar => calendar.Audience.Contains(id))
            .OrderBy(calendar => calendar.Name)
            .Select(calendar => $"the {calendar.Name} calendar")
            .ToListAsync(cancellationToken);
    }
}
