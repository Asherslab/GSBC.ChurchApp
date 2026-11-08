using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarScopeObjects(EventsDbContext db) : IScopeObjects
{
    public ScopeKind Kind => ScopeKind.Calendar;

    public async ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid id) && await db.Calendars.AnyAsync(calendar => calendar.Id == id, cancellationToken);
}

internal sealed class RegularEventScopeObjects(EventsDbContext db) : IScopeObjects
{
    public ScopeKind Kind => ScopeKind.RegularEvent;

    public async ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid id) && await db.RegularEvents.AnyAsync(regular => regular.Id == id, cancellationToken);
}
