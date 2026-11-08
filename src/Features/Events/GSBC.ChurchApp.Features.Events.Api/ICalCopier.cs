using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class ICalCopier(EventsDbContext db, ICalFetcher fetcher, EventsPublisher publisher, TimeProvider time, ILogger<ICalCopier> logger)
{
    public const string UntitledEvent = "Event";
    public const string MoreLink = "More about it";
    private const int LongestDescription = 5_000;
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    public async Task CopyAllAsync(CancellationToken cancellationToken)
    {
        List<Guid> copied = await db.Calendars.Where(calendar => calendar.ICalSource != null && calendar.ICalSource != "")
            .Select(calendar => calendar.Id).ToListAsync(cancellationToken);
        foreach (Guid calendarId in copied)
            await CopyAsync(calendarId, cancellationToken);
        if (copied.Count > 0)
            await publisher.PublishAsync(cancellationToken);
    }

    private async Task CopyAsync(Guid calendarId, CancellationToken cancellationToken)
    {
        if (await db.Calendars.SingleOrDefaultAsync(found => found.Id == calendarId, cancellationToken) is not { } calendar)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        (DateOnly from, DateOnly to) = EventsBook.Window(time);
        bool windowMoved = calendar.ICalCopiedTo != to;
        try
        {
            Uri address = ICalFetcher.AddressOf(calendar.ICalSource!) ?? throw new FormatException("That isn't a web address.");
            FetchedCalendar fetched = await fetcher.FetchAsync(address,
                windowMoved ? null : calendar.ICalETag, windowMoved ? null : calendar.ICalLastModified, cancellationToken);
            if (fetched.Changed)
            {
                await TakeAsync(calendar, ICalFeed.Expand(ICalFeed.Read(fetched.Text), from, to), now, cancellationToken);
                (calendar.ICalETag, calendar.ICalLastModified, calendar.ICalCopiedTo) = (fetched.ETag, fetched.LastModified, to);
            }
            calendar.ICalProblem = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or FormatException or InvalidDataException
                                              or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Copying the calendar {Calendar} from its iCal address failed; keeping what it had", calendar.Name);
            db.ChangeTracker.Entries<EventRecord>().Where(entry => entry.State != EntityState.Unchanged).ToList().ForEach(entry => entry.State = EntityState.Detached);
            calendar.ICalProblem = Problem(exception);
        }
        calendar.ICalReadAt = now;
        publisher.MarkAdmin(calendar.Id, calendar.Id);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task TakeAsync(CalendarRecord calendar, IReadOnlyList<CopiedEvent> copies, DateTime now, CancellationToken cancellationToken)
    {
        Dictionary<string, EventRecord> known = await db.Events.Where(record => record.CalendarId == calendar.Id && record.SourceUid != null)
            .ToDictionaryAsync(record => record.SourceUid!, cancellationToken);
        HashSet<string> wanted = [];
        foreach (CopiedEvent copy in copies.DistinctBy(copy => copy.SourceUid))
        {
            wanted.Add(copy.SourceUid);
            if (!known.TryGetValue(copy.SourceUid, out EventRecord? record))
            {
                record = new EventRecord { Id = Guid.CreateVersion7(), CalendarId = calendar.Id, SourceUid = copy.SourceUid, CreatedAt = now };
                db.Events.Add(record);
            }
            Fill(record, copy);
            record.Showing = Showing.Shown;
            record.UpdatedAt = now;
        }
        db.Events.RemoveRange(known.Values.Where(record => !wanted.Contains(record.SourceUid!)));
    }

    public static void Fill(EventRecord record, CopiedEvent copy)
    {
        ICalEvent source = copy.Source;
        record.Fields = new EventFields
        {
            Title = Shortened(source.Summary.Length > 0 ? source.Summary : UntitledEvent, EventWriter.LongestTitle),
            Description = source.Description.Length > 0 ? new RichText { Markdown = Shortened(source.Description, LongestDescription) } : null,
            Place = source.Location.Length > 0 ? new EventPlace { Address = source.Location } : null,
            Links = Uri.TryCreate(source.Url, UriKind.Absolute, out Uri? url) ? [new EventLink { Title = MoreLink, Url = url.ToString() }] : [],
        };
        record.StartDate = DateOnly.FromDateTime(copy.Start);
        if (copy.AllDay)
        {
            DateOnly last = DateOnly.FromDateTime(copy.End > copy.Start ? copy.End.AddDays(-1) : copy.Start);
            (record.EndDate, record.StartMinute, record.EndMinute) = (last > record.StartDate ? last : null, null, null);
            return;
        }
        record.StartMinute = (int)copy.Start.TimeOfDay.TotalMinutes;
        record.EndMinute = (int)copy.End.TimeOfDay.TotalMinutes;
        record.EndDate = copy.End - copy.Start >= OneDay ? DateOnly.FromDateTime(copy.End) : null;
    }

    private static string Shortened(string text, int longest) => text.Length <= longest ? text : text[..(longest - 1)].TrimEnd() + "…";

    private static string Problem(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: { } status } => $"The calendar's address answered {(int)status}.",
        HttpRequestException => "The calendar's address couldn't be reached.",
        TaskCanceledException => "The calendar's address took too long to answer.",
        _ => exception.Message,
    };
}
