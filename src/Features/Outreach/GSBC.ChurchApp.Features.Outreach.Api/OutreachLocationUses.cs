using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Platform.Api.Locations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachLocationUses(OutreachDbContext db) : ILocationUses
{
    public async Task<IReadOnlyCollection<string>> MapKeysAsync(CancellationToken cancellationToken) =>
        [.. (await db.Days.AsNoTracking().Where(day => day.Meeting != null).Select(day => day.Meeting).ToListAsync(cancellationToken))
            .Select(meeting => meeting?.Map?.Key ?? "").Where(key => key.Length > 0).Distinct()];
}
