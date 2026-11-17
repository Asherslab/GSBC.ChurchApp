using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Platform.Api.Locations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventLocationUses(EventsDbContext db) : ILocationUses
{
    public async Task<IReadOnlyCollection<string>> MapKeysAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQuery<string>($"""
            SELECT DISTINCT key AS "Value" FROM (
                SELECT fields->'place'->'location'->'map'->>'key' AS key FROM events.events
                UNION ALL SELECT fields->'place'->'location'->'map'->>'key' FROM events.regular_events
                UNION ALL SELECT fields->'place'->'location'->'map'->>'key' FROM events.specials) used
            WHERE key IS NOT NULL AND key <> ''
            """).ToListAsync(cancellationToken);
}
