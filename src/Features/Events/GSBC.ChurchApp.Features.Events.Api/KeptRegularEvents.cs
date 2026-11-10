using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class KeptRegularEvents(EventsDbContext db, EventsPublisher publisher, IAccessEvaluator access, IRoleAssignments roles, TimeProvider time)
    : IKeptRegularEvents
{
    public async ValueTask<KeptCalendar?> WhereAsync(string keeper, CancellationToken cancellationToken) =>
        await db.RegularEvents.AsNoTracking().Where(record => record.Keeper == keeper)
            .Join(db.Calendars.AsNoTracking(), record => record.CalendarId, calendar => calendar.Id, (_, calendar) => new KeptCalendar(calendar.Id, calendar.Name))
            .SingleOrDefaultAsync(cancellationToken);

    public async ValueTask<IReadOnlyList<KeptCalendar>> ChoicesAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        List<KeptCalendar> choices = [];
        foreach (CalendarRecord calendar in await db.Calendars.AsNoTracking().Where(calendar => calendar.ICalSource == null || calendar.ICalSource == "")
                     .OrderBy(calendar => calendar.Order).ToListAsync(cancellationToken))
            if (await MayKeepInAsync(user, calendar, cancellationToken))
                choices.Add(new KeptCalendar(calendar.Id, calendar.Name));
        return choices;
    }

    public async ValueTask PutInAsync(ClaimsPrincipal user, Guid calendarId, KeptRegularEvent wanted, CancellationToken cancellationToken)
    {
        CalendarRecord calendar = await db.Calendars.AsNoTracking().SingleOrDefaultAsync(found => found.Id == calendarId, cancellationToken)
                                  ?? throw new RpcException(new Status(StatusCode.NotFound, "That calendar isn't here any more."));
        if (calendar.ICalSource is { Length: > 0 } || !await MayKeepInAsync(user, calendar, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, $"You can't add to {calendar.Name}: choose a calendar you edit."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        RegularEventRecord record = await db.RegularEvents.SingleOrDefaultAsync(found => found.Keeper == wanted.Keeper, cancellationToken)
                                    ?? db.RegularEvents.Add(new RegularEventRecord { Id = Guid.CreateVersion7(), Keeper = wanted.Keeper, CalendarId = calendarId, CreatedAt = now }).Entity;
        if (record.CalendarId != calendarId)
        {
            publisher.MarkAdmin(record.CalendarId, record.Id);
            record.CalendarId = calendarId;
        }
        await KeepAsync(record, wanted, now, cancellationToken);
    }

    public async ValueTask KeepInStepAsync(KeptRegularEvent wanted, CancellationToken cancellationToken)
    {
        if (await db.RegularEvents.SingleOrDefaultAsync(found => found.Keeper == wanted.Keeper, cancellationToken) is { } record)
            await KeepAsync(record, wanted, time.GetUtcNow().UtcDateTime, cancellationToken);
    }

    public async ValueTask RemoveAsync(string keeper, CancellationToken cancellationToken)
    {
        if (await db.RegularEvents.AsNoTracking().SingleOrDefaultAsync(found => found.Keeper == keeper, cancellationToken) is not { } record)
            return;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Showings.Where(showing => showing.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.DetailsReminders.Where(reminder => reminder.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Specials.Where(special => special.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Breaks.Where(stop => stop.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.RegularEvents.Where(found => found.Id == record.Id).ExecuteDeleteAsync(cancellationToken);
            publisher.MarkAdmin(record.CalendarId, record.Id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        await publisher.PublishAsync(cancellationToken);
        await roles.ForgetScopeAsync(ScopeKind.RegularEvent, [record.Id.ToString()], cancellationToken);
    }

    private async Task KeepAsync(RegularEventRecord record, KeptRegularEvent wanted, DateTime now, CancellationToken cancellationToken)
    {
        KeptShape shape = KeptShape.Of(record.Id, wanted, now);
        record.Fields = new EventFields { Title = wanted.Title, Description = wanted.About.Length > 0 ? new RichText { Markdown = wanted.About } : null };
        (record.KeptBy, record.OpensAt) = (wanted.KeptBy, wanted.OpensAt);
        record.Pattern = shape.Pattern;
        (record.StartMinute, record.EndMinute) = (shape.StartMinute, shape.EndMinute);
        (record.Showing, record.ShowFrom, record.AskedAt, record.AskedBy) = (Showing.Shown, null, null, null);
        (record.Needs, record.RemindDaysBefore) = ([], []);
        record.UpdatedAt = now;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Specials.Where(special => special.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.Breaks.Where(stop => stop.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            await db.RegularEventBreaks.Where(link => link.RegularEventId == record.Id).ExecuteDeleteAsync(cancellationToken);
            db.Specials.AddRange(shape.Specials);
            db.Breaks.AddRange(shape.Breaks);
            publisher.MarkAdmin(record.CalendarId, record.Id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        await publisher.PublishAsync(cancellationToken);
    }

    private async Task<bool> MayKeepInAsync(ClaimsPrincipal user, CalendarRecord calendar, CancellationToken cancellationToken) =>
        await access.IsAllowedAsync(user, EventsPermissions.EditNode, calendar.Id.ToString(), cancellationToken)
        && (!calendar.NeedsPublisher || await access.IsAllowedAsync(user, EventsPermissions.PublishNode, calendar.Id.ToString(), cancellationToken));
}
