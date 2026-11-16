using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class MinistryCalendars(
    EventsDbContext db, EventsPublisher publisher, CalendarRemover remover, CalendarFollowing following, IRoleAssignments roles, IServiceProvider services, TimeProvider time)
    : IMinistryCalendars
{
    public async ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken) =>
        (await db.Calendars.AsNoTracking().Where(calendar => calendar.MinistryId == ministryId).OrderBy(calendar => calendar.Order).ToListAsync(cancellationToken))
        .Select(Of)
        .ToList();

    public async ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
        (await db.Calendars.AsNoTracking().Where(calendar => calendar.MinistryId != null && ministryIds.Contains(calendar.MinistryId.Value))
            .OrderBy(calendar => calendar.Order).ToListAsync(cancellationToken))
        .Select(Of)
        .ToList();

    public async ValueTask<IReadOnlyList<MinistryCalendar>> ForEveryoneAsync(CancellationToken cancellationToken) =>
        (await db.Calendars.AsNoTracking().OrderBy(calendar => calendar.Order).ToListAsync(cancellationToken))
        .Select(Of)
        .Where(calendar => !calendar.Hidden && calendar.Audience.Text == Audience.Everyone.Text)
        .ToList();

    public async ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken)
    {
        List<CalendarRecord> owned = await db.Calendars.Where(calendar => calendar.MinistryId == ministryId).ToListAsync(cancellationToken);
        List<CalendarRecord> changing = owned.Where(calendar => CalendarHiding.IsHidden(calendar) == shown).ToList();
        if (changing.Count == 0)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        foreach (CalendarRecord calendar in changing)
        {
            if (shown)
                CalendarHiding.Show(calendar);
            else
                CalendarHiding.Hide(calendar, whileHidden.Text);
            calendar.UpdatedAt = now;
            publisher.MarkAdmin(calendar.Id, calendar.Id);
        }
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    public async ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken)
    {
        string wanted = Named(name);
        DateTime now = time.GetUtcNow().UtcDateTime;
        CalendarRecord calendar = new()
        {
            Id = Guid.CreateVersion7(), Name = wanted, Slug = await FreeSlugAsync(CalendarLinks.SlugOf(wanted), null, cancellationToken),
            Description = "", Audience = audience.Text, MinistryId = ministryId, CreatedAt = now, UpdatedAt = now,
            Order = await db.Calendars.Select(other => (int?)other.Order).MaxAsync(cancellationToken) + 1 ?? 0,
        };
        db.Calendars.Add(calendar);
        publisher.MarkAdmin(calendar.Id, calendar.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
        return Of(calendar);
    }

    public async ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken)
    {
        CalendarRecord calendar = await OwnedAsync(ministryId, calendarId, cancellationToken);
        string wanted = Named(name);
        if (wanted != calendar.Name)
            calendar.Slug = await FreeSlugAsync(CalendarLinks.SlugOf(wanted), calendar.Id, cancellationToken);
        (calendar.Name, calendar.UpdatedAt) = (wanted, time.GetUtcNow().UtcDateTime);
        CalendarHiding.Choose(calendar, audience.Text);
        publisher.MarkAdmin(calendar.Id, calendar.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return Of(calendar);
    }

    public async ValueTask<MinistryCalendarRemoval> WhatGoesAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken)
    {
        await OwnedAsync(ministryId, calendarId, cancellationToken);
        CalendarRemoval removal = await remover.WhatGoesAsync(calendarId.ToString(), cancellationToken);
        return new MinistryCalendarRemoval(removal.Name, removal.Events, removal.RegularEvents);
    }

    public async ValueTask RemoveAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken)
    {
        await OwnedAsync(ministryId, calendarId, cancellationToken);
        await remover.RemoveAsync(calendarId.ToString(), cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
    }

    public async ValueTask MergeAsync(Guid ministryId, Guid fromCalendarId, Guid intoCalendarId, CancellationToken cancellationToken)
    {
        CalendarRecord from = await OwnedAsync(ministryId, fromCalendarId, cancellationToken);
        CalendarRecord into = await OwnedAsync(ministryId, intoCalendarId, cancellationToken);
        if (from.Id == into.Id)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            List<EventRecord> events = await db.Events.Where(record => record.CalendarId == from.Id).ToListAsync(cancellationToken);
            List<RegularEventRecord> regulars = await db.RegularEvents.Where(record => record.CalendarId == from.Id).ToListAsync(cancellationToken);
            foreach (EventRecord record in events)
                (record.CalendarId, record.UpdatedAt) = (into.Id, now);
            foreach (RegularEventRecord record in regulars)
                (record.CalendarId, record.UpdatedAt) = (into.Id, now);
            List<Guid> moved = [.. events.Select(record => record.Id), .. regulars.Select(record => record.Id)];
            List<Guid> native = [
                .. await db.Events.Where(record => record.CalendarId == into.Id).Select(record => record.Id).ToListAsync(cancellationToken),
                .. await db.RegularEvents.Where(record => record.CalendarId == into.Id).Select(record => record.Id).ToListAsync(cancellationToken)];
            List<ShowingRecord> showings = await db.Showings.Where(showing => showing.CalendarId == from.Id || showing.CalendarId == into.Id).ToListAsync(cancellationToken);
            foreach (ShowingRecord showing in showings)
            {
                Guid item = showing.EventId ?? showing.RegularEventId ?? Guid.Empty;
                bool alreadyThere = showing.CalendarId == from.Id
                    && showings.Any(other => other.CalendarId == into.Id && other.EventId == showing.EventId && other.RegularEventId == showing.RegularEventId);
                if (moved.Contains(item) || native.Contains(item) || alreadyThere)
                    db.Showings.Remove(showing);
                else
                    showing.CalendarId = into.Id;
            }
            foreach (DigestRecord digest in await db.Digests.ToListAsync(cancellationToken))
                if (MergedDigest(digest, from.Id, into.Id))
                {
                    db.Entry(digest).Property(record => record.CalendarIds).IsModified = true;
                    db.Entry(digest).Property(record => record.Sent).IsModified = true;
                }
            db.Calendars.Remove(from);
            into.UpdatedAt = now;
            publisher.MarkAdmin(from.Id, from.Id);
            publisher.MarkAdmin(into.Id, into.Id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        await publisher.PublishAsync(cancellationToken);
        await roles.MoveScopeAsync(ScopeKind.Calendar, from.Id.ToString(), into.Id.ToString(), cancellationToken);
        await roles.ScopesChangedAsync(cancellationToken);
        await following.PublishAsync(null, cancellationToken);
        foreach (ICalendarMerges merge in services.GetServices<ICalendarMerges>())
            await merge.MergedAsync(from.Id, into.Id, cancellationToken);
    }

    internal static bool MergedDigest(DigestRecord digest, Guid from, Guid into)
    {
        bool changed = false;
        if (digest.CalendarIds is { } chosen && chosen.Contains(from))
        {
            digest.CalendarIds = chosen.Select(id => id == from ? into : id).Distinct().ToList();
            changed = true;
        }
        foreach (SentDigestItem item in digest.Sent.Items.Where(item => item.CalendarIds.Contains(from)))
        {
            List<Guid> ids = item.CalendarIds.Select(id => id == from ? into : id).Distinct().ToList();
            item.CalendarIds.Clear();
            item.CalendarIds.AddRange(ids);
            changed = true;
        }
        return changed;
    }

    private static string Named(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length is > 0 and <= CalendarSetupService.LongestName
            ? trimmed
            : throw new RpcException(new Status(StatusCode.InvalidArgument, $"Give the calendar a name of up to {CalendarSetupService.LongestName} characters."));
    }

    private async Task<CalendarRecord> OwnedAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) =>
        await db.Calendars.SingleOrDefaultAsync(calendar => calendar.Id == calendarId && calendar.MinistryId == ministryId, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That calendar isn't this ministry's any more."));

    private async Task<string> FreeSlugAsync(string slug, Guid? self, CancellationToken cancellationToken)
    {
        string root = CalendarLinks.IsValidSlug(slug) ? slug : "ministry";
        for (int suffix = 1; ; suffix++)
        {
            string candidate = suffix == 1 ? root : $"{root}-{suffix}";
            if (!await db.Calendars.AnyAsync(other => other.Slug == candidate && other.Id != self, cancellationToken))
                return candidate;
        }
    }

    private static MinistryCalendar Of(CalendarRecord calendar) =>
        new(calendar.Id, calendar.MinistryId ?? Guid.Empty, calendar.Name, calendar.Slug, CalendarAudiences.Of(CalendarHiding.Chosen(calendar)), CalendarHiding.IsHidden(calendar));
}

internal sealed class MinistryCalendarAttachments(EventsDbContext db) : IScopeAttachments
{
    public ScopeKind AttachedTo => ScopeKind.Ministry;

    public async ValueTask<IReadOnlyList<Scope>> AttachedAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid ministry)
            ? (await db.Calendars.AsNoTracking().Where(calendar => calendar.MinistryId == ministry).Select(calendar => calendar.Id).ToListAsync(cancellationToken))
                .Select(id => Scope.Of(ScopeKind.Calendar, id.ToString()))
                .ToList()
            : [];
}
