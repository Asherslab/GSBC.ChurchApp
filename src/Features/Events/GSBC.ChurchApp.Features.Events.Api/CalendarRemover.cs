using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarRemover(EventsDbContext db, EventsPublisher publisher, IRoleAssignments roles, IMediaReferences media)
{
    public async Task<CalendarRemoval> WhatGoesAsync(string calendarId, CancellationToken cancellationToken)
    {
        Guid id = await ExistingAsync(calendarId, cancellationToken);
        CalendarRemoval removal = WhatGoes(await EventsBook.LoadAsync(db, cancellationToken), id);
        removal.AddedBy = await roles.HoldingCountAsync(EventsPermissions.AdderRole, id.ToString(), cancellationToken);
        return removal;
    }

    public static CalendarRemoval WhatGoes(EventsBook book, Guid calendarId)
    {
        CalendarRecord calendar = book.Calendars[calendarId];
        List<IShowable> its = book.Events.Cast<IShowable>().Concat(book.RegularEvents).Where(item => item.CalendarId == calendarId).ToList();
        return new CalendarRemoval
        {
            Name = calendar.Name,
            Events = its.Count(item => item is EventRecord),
            RegularEvents = its.Count(item => item is RegularEventRecord),
            ShownElsewhere = its.Count(item => book.AlsoOn(item.Id).Any(other => other != calendarId && book.Calendars.ContainsKey(other))),
            FromElsewhere = book.Events.Cast<IShowable>().Concat(book.RegularEvents)
                .Count(item => item.CalendarId != calendarId && book.AlsoOn(item.Id).Contains(calendarId)),
            ShownOnNewPhones = calendar.ShownOnNewPhones,
            ForEveryone = CalendarAudiences.Of(calendar.Audience) == Audience.Everyone,
        };
    }

    public async Task RemoveAsync(string calendarId, CancellationToken cancellationToken)
    {
        Guid id = await ExistingAsync(calendarId, cancellationToken);
        List<Guid> events = await db.Events.Where(record => record.CalendarId == id).Select(record => record.Id).ToListAsync(cancellationToken);
        List<Guid> regulars = await db.RegularEvents.Where(record => record.CalendarId == id).Select(record => record.Id).ToListAsync(cancellationToken);
        List<SpecialRecord> specials = await db.Specials.AsNoTracking().Where(special => regulars.Contains(special.RegularEventId)).ToListAsync(cancellationToken);

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Showings.Where(showing => showing.CalendarId == id
                || (showing.EventId != null && events.Contains(showing.EventId.Value))
                || (showing.RegularEventId != null && regulars.Contains(showing.RegularEventId.Value))).ExecuteDeleteAsync(cancellationToken);
            await db.DetailsReminders.Where(reminder => regulars.Contains(reminder.RegularEventId)).ExecuteDeleteAsync(cancellationToken);
            await db.Specials.Where(special => regulars.Contains(special.RegularEventId)).ExecuteDeleteAsync(cancellationToken);
            await db.RegularEventBreaks.Where(link => regulars.Contains(link.RegularEventId)).ExecuteDeleteAsync(cancellationToken);
            await db.Breaks.Where(stop => stop.RegularEventId != null && regulars.Contains(stop.RegularEventId.Value)).ExecuteDeleteAsync(cancellationToken);
            await db.RegularEvents.Where(record => record.CalendarId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Events.Where(record => record.CalendarId == id).ExecuteDeleteAsync(cancellationToken);
            await db.Calendars.Where(calendar => calendar.Id == id).ExecuteDeleteAsync(cancellationToken);
            foreach (DigestRecord digest in await db.Digests.Where(digest => digest.CalendarIds != null).ToListAsync(cancellationToken))
                if (digest.CalendarIds!.Remove(id))
                    db.Entry(digest).Property(record => record.CalendarIds).IsModified = true;
            publisher.MarkAdmin(id, id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await publisher.PublishAsync(cancellationToken);
        foreach (Guid regular in regulars)
            await media.UseAsync(EventWriter.RegularOwner(regular), [], cancellationToken);
        foreach (SpecialRecord special in specials)
            await media.UseAsync(EventWriter.SpecialOwner(special.RegularEventId, special.Date), [], cancellationToken);
        foreach (Guid record in events)
            await media.UseAsync(EventWriter.EventOwner(record), [], cancellationToken);
        await roles.ForgetScopeAsync(ScopeKind.RegularEvent, regulars.Select(regular => regular.ToString()).ToList(), cancellationToken);
        await roles.ForgetScopeAsync(ScopeKind.Calendar, [id.ToString()], cancellationToken);
    }

    private async Task<Guid> ExistingAsync(string calendarId, CancellationToken cancellationToken) =>
        Guid.TryParse(calendarId, out Guid id) && await db.Calendars.AnyAsync(calendar => calendar.Id == id, cancellationToken)
            ? id
            : throw new RpcException(new Status(StatusCode.NotFound, "That calendar has already been removed."));
}
