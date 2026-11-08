using System.Globalization;
using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class EventsPublisher(EventsDbContext db, TimeProvider time)
{
    private const long AdvisoryLockKey = 0x6576_656e_7473_7075;

    public static readonly IReadOnlyList<string> EachRowFeeds = [EventsFeeds.PrivateCalendars, EventsFeeds.Private, EventsFeeds.Contacts, EventsFeeds.PrivateDigest];
    private static readonly IReadOnlyList<string> Published = [EventsFeeds.Calendars, .. EachRowFeeds, EventsFeeds.Public, EventsFeeds.Digest];
    private const string DigestHeader = "digest";

    internal sealed record Row(string Feed, string RecordId, byte[] Payload, string? Audience, DateTime? StartsAt = null, DateTime? EndsAt = null, string? SortKey = null, string? GroupKey = null);

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cancellationToken);

        DateTime now = time.GetUtcNow().UtcDateTime;
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        DigestRecord? digest = await db.Digests.AsNoTracking().Where(sent => sent.SentAt != null).OrderByDescending(sent => sent.SentAt).FirstOrDefaultAsync(cancellationToken);
        Dictionary<(string, string), Row> wanted = Rows(book, now).Concat(DigestRows(book, digest)).ToDictionary(row => (row.Feed, row.RecordId));
        Dictionary<(string, string), PublishedRow> published = await db.PublishedRows
            .Where(row => Published.Contains(row.Feed)).ToDictionaryAsync(row => (row.Feed, row.RecordId), cancellationToken);

        long version = now.Ticks;
        List<FeedChangeRecord> tombstones = [];
        List<FeedChangeRecord> changes = [];
        foreach ((var key, PublishedRow before) in published.Where(pair => !wanted.ContainsKey(pair.Key)))
        {
            tombstones.Add(Tombstone(before, version));
            db.PublishedRows.Remove(before);
        }

        foreach ((var key, Row row) in wanted)
        {
            string hash = Convert.ToHexStringLower(SHA256.HashData(row.Payload));
            published.TryGetValue(key, out PublishedRow? before);
            if (before is not null && before.Hash == hash && before.Audience == row.Audience)
                continue;
            if (before is not null && before.Audience != row.Audience)
                tombstones.Add(Tombstone(before, version));
            if (before is null)
                db.PublishedRows.Add(new PublishedRow { Feed = row.Feed, RecordId = row.RecordId, Hash = hash, Audience = row.Audience });
            else
            {
                before.Hash = hash;
                before.Audience = row.Audience;
            }
            changes.Add(new FeedChangeRecord
            {
                Feed = row.Feed, RecordId = row.RecordId, Version = version, Payload = row.Payload, ScopeId = row.Audience,
                StartsAt = row.StartsAt, EndsAt = row.EndsAt, SortKey = row.SortKey, GroupKey = row.GroupKey,
            });
        }

        db.FeedChanges.AddRange(tombstones);
        await db.SaveChangesAsync(cancellationToken);
        db.FeedChanges.AddRange(changes);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public void MarkAdmin(Guid calendarId, Guid itemId) =>
        db.FeedChanges.Add(new FeedChangeRecord
        {
            Feed = EventsFeeds.Admin, RecordId = itemId.ToString(), Version = time.GetUtcNow().UtcDateTime.Ticks, ScopeId = calendarId.ToString(),
        });

    public static IEnumerable<Row> Rows(EventsBook book, DateTime now)
    {
        foreach (CalendarRecord calendar in book.Calendars.Values)
        {
            Audience audience = CalendarAudiences.Of(calendar.Audience);
            byte[] payload = EventsPayloads.Encode(CalendarPayload(calendar));
            yield return audience == Audience.Everyone
                ? new Row(EventsFeeds.Calendars, calendar.Id.ToString(), payload, null, SortKey: SortKey(calendar))
                : new Row(EventsFeeds.PrivateCalendars, calendar.Id.ToString(), payload, audience.Text, SortKey: SortKey(calendar));
        }

        (DateOnly from, DateOnly to) = WindowAt(now);
        foreach (EventOccurrence occurrence in book.Occurrences(from, to, item => EventsBook.IsShown(item, now)))
        {
            Audience audience = book.AudienceOf(occurrence);
            List<EventContact> contacts = occurrence.Fields.Contacts;
            EventOccurrence payload = ForPhones(occurrence, now);
            string sortKey = occurrence.StartsAt.ToString("O");
            yield return audience == Audience.Everyone
                ? new Row(EventsFeeds.Public, occurrence.Id, EventsPayloads.Encode(payload), null, occurrence.StartsAt, occurrence.EndsAt, sortKey, occurrence.CalendarId)
                : new Row(EventsFeeds.Private, occurrence.Id, EventsPayloads.Encode(payload), audience.Text, occurrence.StartsAt, occurrence.EndsAt, sortKey, occurrence.CalendarId);
            if (contacts.Any(contact => contact.Phone.Length > 0 || contact.Email.Length > 0))
                yield return new Row(EventsFeeds.Contacts, occurrence.Id,
                    EventsPayloads.Encode(new OccurrenceContacts { OccurrenceId = occurrence.Id, Contacts = contacts }),
                    CalendarAudiences.SignedInPart(audience).Text, occurrence.StartsAt, occurrence.EndsAt, sortKey, occurrence.CalendarId);
        }
    }

    public static IEnumerable<Row> DigestRows(EventsBook book, DigestRecord? digest)
    {
        if (digest is not { SentAt: { } sentAt })
            yield break;
        string start = EventDates.Format(digest.Start);
        yield return new Row(EventsFeeds.Digest, DigestHeader,
            EventsPayloads.Encode(new DigestShown { Start = start, End = EventDates.Format(digest.Sent.End), SentAt = sentAt }), null);
        foreach (SentDigestItem sent in digest.Sent.Items)
        {
            Audience audience = Digests.AudienceOf(book, sent);
            string recordId = $"{start}/{sent.Item.Key}";
            byte[] payload = EventsPayloads.Encode(sent.Item);
            string sortKey = sent.Item.Order.ToString("D4", CultureInfo.InvariantCulture);
            yield return audience == Audience.Everyone
                ? new Row(EventsFeeds.Digest, recordId, payload, null, SortKey: sortKey)
                : new Row(EventsFeeds.PrivateDigest, recordId, payload, audience.Text, SortKey: sortKey);
        }
    }

    public static (DateOnly From, DateOnly To) WindowAt(DateTime now) => EventsBook.Window(new FixedTime(now));

    public static CalendarInfo CalendarPayload(CalendarRecord calendar) => new()
    {
        Id = calendar.Id.ToString(),
        Name = calendar.Name,
        Slug = calendar.Slug,
        Description = calendar.Description,
        Audience = CalendarAudiences.Of(calendar.Audience).Text,
        ShownOnNewPhones = calendar.ShownOnNewPhones,
        ReadOnly = calendar.ICalSource is { Length: > 0 },
        MinistryId = calendar.MinistryId?.ToString(),
        Order = calendar.Order,
    };

    private static EventOccurrence ForPhones(EventOccurrence occurrence, DateTime now)
    {
        bool recentlyChanged = occurrence.ChangedAt is { } at && now - at < EventsFeeds.ChangedFor;
        return new EventOccurrence
        {
            Id = occurrence.Id,
            Kind = occurrence.Kind,
            ItemId = occurrence.ItemId,
            Date = occurrence.Date,
            EndDate = occurrence.EndDate,
            CalendarId = occurrence.CalendarId,
            AlsoOn = occurrence.AlsoOn,
            Fields = new EventFields
            {
                Title = occurrence.Fields.Title,
                Label = occurrence.Fields.Label,
                Picture = occurrence.Fields.Picture,
                Description = occurrence.Fields.Description,
                Place = occurrence.Fields.Place,
                Contacts = occurrence.Fields.Contacts.Select(contact => new EventContact { PersonId = contact.PersonId, Name = contact.Name }).ToList(),
                RegisterLink = occurrence.Fields.RegisterLink,
                Links = occurrence.Fields.Links,
            },
            StartsAt = occurrence.StartsAt,
            EndsAt = occurrence.EndsAt,
            AllDay = occurrence.AllDay,
            Repeats = occurrence.Repeats,
            NotOn = occurrence.NotOn,
            NotOnReason = occurrence.NotOnReason,
            BackOn = occurrence.BackOn,
            Missing = occurrence.Missing,
            ChangedAt = recentlyChanged ? occurrence.ChangedAt : null,
            ChangeNote = recentlyChanged ? occurrence.ChangeNote : "",
            ListEveryDate = occurrence.ListEveryDate,
        };
    }

    private static string SortKey(CalendarRecord calendar) => $"{calendar.Order:D6}|{calendar.Name}";

    private static FeedChangeRecord Tombstone(PublishedRow row, long version) =>
        new() { Feed = row.Feed, RecordId = row.RecordId, Version = version, Deleted = true };

    private sealed class FixedTime(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }
}

internal static class EventsPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        EventsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return EventsContractsModel.Instance.Deserialize<T>(stream);
    }
}
