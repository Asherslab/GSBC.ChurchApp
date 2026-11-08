using System.Security.Claims;
using System.Text.Json;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class DigestService(EventsDbContext db, EventsPublisher publisher, INotifications notifications, TimeProvider time) : IDigestService
{
    private const int LongestWords = 200;
    private const int LongestDescription = 2000;
    private const int MostEntries = 60;

    public async ValueTask<DigestDraft> OpenAsync(DigestQuery request, CallContext context = default)
    {
        DateOnly start = StartOf(request.Start, required: false);
        DigestRecord? record = await db.Digests.AsNoTracking().SingleOrDefaultAsync(digest => digest.Start == start, context.CancellationToken);
        return await DraftAsync(start, record, context.CancellationToken);
    }

    public async ValueTask<DigestDraft> SaveAsync(DigestSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateOnly start = StartOf(request.Start, required: true);
        List<DigestEntry> entries = Checked(request.Entries);
        DigestRecord record = await RecordAsync(start, cancellationToken);
        record.Weeks = Math.Clamp(request.Weeks, 1, Digests.MostWeeks);
        record.CalendarIds = request.CalendarIds.Select(id => Guid.TryParse(id, out Guid calendarId) ? calendarId : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
        record.Entries = entries;
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        record.UpdatedBy = Caller.UserId(UserOf(context));
        await db.SaveChangesAsync(cancellationToken);
        return await DraftAsync(start, record, cancellationToken);
    }

    public async ValueTask<DigestDraft> SendToAppAsync(DigestRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateOnly start = StartOf(request.Start, required: true);
        DigestRecord record = await RecordAsync(start, cancellationToken);
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        SentDigest sent = Digests.ForApp(ItemsOf(book, record), Digests.EndOf(start, WeeksOf(record)));
        if (sent.Items.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Tick at least one thing to send."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        record.Sent = sent;
        record.SentAt = now;
        record.SentBy = Caller.UserId(UserOf(context));
        if (record.UpdatedAt == default)
            record.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        return await DraftAsync(start, record, cancellationToken);
    }

    public async ValueTask<Done> SendToMyPhoneAsync(DigestRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DateOnly start = StartOf(request.Start, required: true);
        DigestRecord? latest = await db.Digests.AsNoTracking().Where(digest => digest.SentAt != null).OrderByDescending(digest => digest.SentAt).FirstOrDefaultAsync(cancellationToken);
        if (latest is null || latest.Start != start)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Send it to the app first: the notification opens what the app has."));
        if (Caller.UserId(UserOf(context)) is not { } me)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in to send it to your phone."));

        await notifications.RaiseAsync(Digests.Notification(latest.Sent, NotificationAudience.Person(me)) with { Test = true }, cancellationToken);
        return new Done();
    }

    private async Task<DigestDraft> DraftAsync(DateOnly start, DigestRecord? record, CancellationToken cancellationToken)
    {
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        int weeks = WeeksOf(record);
        List<DigestItem> items = ItemsOf(book, record ?? new DigestRecord { Start = start });
        DigestRecord? latest = await db.Digests.AsNoTracking().Where(digest => digest.SentAt != null).OrderByDescending(digest => digest.SentAt).FirstOrDefaultAsync(cancellationToken);
        return new DigestDraft
        {
            Start = EventDates.Format(start),
            End = EventDates.Format(Digests.EndOf(start, weeks)),
            Weeks = weeks,
            CalendarIds = CalendarsOf(book, record).Select(id => id.ToString()).ToList(),
            Calendars = book.Calendars.Values.OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name).Select(EventsPublisher.CalendarPayload).ToList(),
            Items = items,
            SavedAt = record is { UpdatedAt: var saved } && saved != default ? saved : null,
            SentAt = record?.SentAt,
            SentAsItIs = record?.SentAt is not null && Same(Digests.ForApp(items, Digests.EndOf(start, weeks)), record.Sent),
            LastSent = latest is { SentAt: { } at } ? new DigestSent { Start = EventDates.Format(latest.Start), SentAt = at } : null,
        };
    }

    private List<DigestItem> ItemsOf(EventsBook book, DigestRecord record) =>
        Digests.Merge(Digests.Candidates(book, record.Start, WeeksOf(record), CalendarsOf(book, record), time.GetUtcNow().UtcDateTime), record.Entries);

    private static IReadOnlySet<Guid> CalendarsOf(EventsBook book, DigestRecord? record) =>
        record?.CalendarIds is { } chosen ? chosen.Where(book.Calendars.ContainsKey).ToHashSet() : Digests.DefaultCalendars(book);

    private static int WeeksOf(DigestRecord? record) => record is { Weeks: > 0 } ? record.Weeks : Digests.DefaultWeeks;

    private async Task<DigestRecord> RecordAsync(DateOnly start, CancellationToken cancellationToken)
    {
        if (await db.Digests.SingleOrDefaultAsync(digest => digest.Start == start, cancellationToken) is { } existing)
            return existing;
        DigestRecord created = new() { Start = start, Weeks = Digests.DefaultWeeks };
        db.Digests.Add(created);
        return created;
    }

    private DateOnly StartOf(string text, bool required)
    {
        if (EventDates.Parse(text) is { } day)
            return ChurchTime.WeekStart(day);
        if (required)
            throw Invalid("Choose which weeks the digest is for.");
        return Digests.NextStart(ChurchTime.Today(time));
    }

    private static List<DigestEntry> Checked(IReadOnlyList<DigestEntry> entries)
    {
        if (entries.Count > MostEntries)
            throw Invalid($"A digest holds at most {MostEntries} things.");
        if (entries.Select(entry => entry.Key).Distinct().Count() != entries.Count || entries.Any(entry => entry.Key.Length is 0 or > LongestWords))
            throw Invalid("Each thing in the digest needs its own key.");
        foreach (DigestEntry entry in entries)
        {
            if (entry.Own != entry.Key.StartsWith(Digests.OwnPrefix, StringComparison.Ordinal))
                throw Invalid("Something you added needs a key of its own.");
            DigestWords words = entry.Words;
            if (new[] { words.Label, words.Title, words.When, words.Time, words.RegisterLink }.Any(text => text.Length > LongestWords) || words.Description.Length > LongestDescription)
                throw Invalid("That's too long for the digest.");
            if (words.RegisterLink.Length > 0 && !(Uri.TryCreate(words.RegisterLink, UriKind.Absolute, out Uri? link) && link.Scheme is "https" or "http"))
                throw Invalid("A register link is a web address, starting https://");
            if (entry.Own && words.Title.Trim().Length == 0)
                throw Invalid("Give what you added a title.");
        }
        return entries.Select(entry => new DigestEntry
        {
            Key = entry.Key, Included = entry.Included, Own = entry.Own, Words = entry.Words.Copy(),
            Changed = entry.Own ? [.. DigestFields.All] : entry.Changed.Where(DigestFields.All.Contains).Distinct().ToList(),
        }).ToList();
    }

    private static bool Same(SentDigest first, SentDigest second) =>
        JsonSerializer.Serialize(first, JsonSerializerOptions.Web) == JsonSerializer.Serialize(second, JsonSerializerOptions.Web);

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}
