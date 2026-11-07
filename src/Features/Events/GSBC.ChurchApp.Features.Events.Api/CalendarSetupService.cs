using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarSetupService(EventsDbContext db, EventsPublisher publisher, TimeProvider time) : ICalendarSetupService
{
    public const int LongestName = 60;
    public const int LongestDescription = 500;

    public async ValueTask<CalendarSettings> SaveCalendarAsync(CalendarSettings request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        CalendarInfo wanted = request.Calendar;
        string name = wanted.Name.Trim();
        if (name.Length is 0 or > LongestName)
            throw Invalid($"Give the calendar a name of up to {LongestName} characters.");
        if (wanted.Description.Trim().Length > LongestDescription)
            throw Invalid($"Keep the description to {LongestDescription} characters.");
        string slug = wanted.Slug.Trim().Length > 0 ? wanted.Slug.Trim().ToLowerInvariant() : CalendarLinks.SlugOf(name);
        if (!CalendarLinks.IsValidSlug(slug))
            throw Invalid($"The link name uses lowercase letters, numbers and dashes, and can't be “{CalendarLinks.Public}” or “{CalendarLinks.Mine}”.");
        if (!Audience.TryParse(wanted.Audience, out Audience? audience) || audience.Node is not null)
            throw Invalid("Choose who the calendar is for: everyone, anyone signed in, or people in chosen roles.");
        string source = request.ICalSource.Trim();
        if (source.Length > 0 && !IsCalendarAddress(source))
            throw Invalid("The calendar to copy needs to be a web address (https:// or webcal://).");
        Guid? ministry = wanted.MinistryId is { Length: > 0 } ministryText
            ? Guid.TryParse(ministryText, out Guid ministryId) ? ministryId : throw Invalid("That ministry isn't known.")
            : null;

        Guid wantedId = Guid.TryParse(wanted.Id, out Guid parsedId) ? parsedId : Guid.Empty;
        CalendarRecord? calendar = wanted.Id.Length > 0
            ? await db.Calendars.SingleOrDefaultAsync(found => found.Id == wantedId, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That calendar isn't here any more."))
            : null;
        Guid self = calendar?.Id ?? Guid.Empty;
        if (await db.Calendars.AnyAsync(other => other.Slug == slug && other.Id != self, cancellationToken))
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Another calendar already uses the link name “{slug}”."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        if (calendar is null)
        {
            calendar = new CalendarRecord
            {
                Id = Guid.CreateVersion7(), Name = name, Slug = slug, CreatedAt = now,
                Order = await db.Calendars.Select(other => (int?)other.Order).MaxAsync(cancellationToken) + 1 ?? 0,
            };
            db.Calendars.Add(calendar);
        }

        calendar.Name = name;
        calendar.Slug = slug;
        calendar.Description = wanted.Description.Trim();
        calendar.Audience = audience.Text;
        calendar.ShownOnNewPhones = wanted.ShownOnNewPhones;
        calendar.NeedsPublisher = request.NeedsPublisher;
        calendar.MinistryId = ministry;
        if (calendar.ICalSource != (source.Length > 0 ? source : null))
            (calendar.ICalSource, calendar.ICalReadAt, calendar.ICalProblem) = (source.Length > 0 ? source : null, null, null);
        calendar.UpdatedAt = now;
        publisher.MarkAdmin(calendar.Id, calendar.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return Settings(calendar);
    }

    public async ValueTask<BreakInfo> SaveBreakAsync(BreakInfo request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        string name = request.Name.Trim();
        DateOnly start = EventDates.Parse(request.StartDate) ?? throw Invalid("Choose the break's first date.");
        DateOnly end = EventDates.Parse(request.EndDate) ?? start;
        if (name.Length == 0)
            throw Invalid("Give the break a name, such as “Christmas break”.");
        if (end < start)
            throw Invalid("A break can't end before it starts.");

        Guid wantedId = Guid.TryParse(request.Id, out Guid parsedId) ? parsedId : Guid.Empty;
        BreakRecord? stop = request.Id.Length > 0
            ? await db.Breaks.SingleOrDefaultAsync(found => found.Id == wantedId && found.RegularEventId == null, cancellationToken)
              ?? throw new RpcException(new Status(StatusCode.NotFound, "That break isn't here any more."))
            : null;
        if (stop is null)
        {
            stop = new BreakRecord { Id = Guid.CreateVersion7(), Name = name, CreatedAt = time.GetUtcNow().UtcDateTime };
            db.Breaks.Add(stop);
        }
        (stop.Name, stop.StartDate, stop.EndDate) = (name, start, end);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        List<Guid> users = await db.RegularEventBreaks.AsNoTracking().Where(link => link.BreakId == stop.Id).Select(link => link.RegularEventId).ToListAsync(cancellationToken);
        return Info(stop, users);
    }

    public async ValueTask<Done> DeleteBreakAsync(BreakRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = Guid.TryParse(request.Id, out Guid parsed) ? parsed : Guid.Empty;
        db.RegularEventBreaks.RemoveRange(db.RegularEventBreaks.Where(link => link.BreakId == id));
        db.Breaks.RemoveRange(db.Breaks.Where(stop => stop.Id == id && stop.RegularEventId == null));
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return new Done();
    }

    public static async Task<BreakList> BreaksAsync(EventsDbContext db, CancellationToken cancellationToken)
    {
        List<BreakRecord> named = await db.Breaks.AsNoTracking().Where(stop => stop.RegularEventId == null).OrderBy(stop => stop.StartDate).ToListAsync(cancellationToken);
        ILookup<Guid, Guid> users = (await db.RegularEventBreaks.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(link => link.BreakId, link => link.RegularEventId);
        return new BreakList { Breaks = named.Select(stop => Info(stop, users[stop.Id].ToList())).ToList() };
    }

    public static BreakInfo Info(BreakRecord stop, IReadOnlyList<Guid> usedBy) => new()
    {
        Id = stop.Id.ToString(),
        Name = stop.Name,
        StartDate = EventDates.Format(stop.StartDate),
        EndDate = EventDates.Format(stop.EndDate),
        UsedBy = usedBy.Select(id => id.ToString()).ToList(),
    };

    public static CalendarSettings Settings(CalendarRecord calendar) => new()
    {
        Calendar = EventsPublisher.CalendarPayload(calendar),
        NeedsPublisher = calendar.NeedsPublisher,
        ICalSource = calendar.ICalSource ?? "",
        ICalReadAt = calendar.ICalReadAt,
        ICalProblem = calendar.ICalProblem ?? "",
    };

    private static bool IsCalendarAddress(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && uri.Scheme is "https" or "http" or "webcal";

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
