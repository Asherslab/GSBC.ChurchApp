using System.Security.Claims;
using System.Text.Json;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class DigestService(
    EventsDbContext db, EventsPublisher publisher, INotifications notifications, INewsHighlights news, IMediaReferences media, TimeProvider time) : IDigestService
{
    private const int RecentPosts = 30;
    private const int LongestWords = 200;
    private const int LongestDescription = 2000;
    private const int MostEntries = 60;

    public async ValueTask<DigestDraft> OpenAsync(DigestQuery request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DigestRecord? record = Guid.TryParse(request.Id, out Guid id)
            ? await db.Digests.AsNoTracking().SingleOrDefaultAsync(digest => digest.Id == id, cancellationToken)
            : await CurrentAsync(cancellationToken);
        return await DraftAsync(record ?? Fresh(), cancellationToken);
    }

    public async ValueTask<DigestDraft> SaveAsync(DigestSave request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = IdOf(request.Id);
        DateOnly start = EventDates.Parse(request.Start) ?? throw Invalid("Choose the day the digest starts.");
        if (start < ChurchTime.Today(time).AddYears(-1) || start > ChurchTime.Today(time).AddYears(1))
            throw Invalid("Choose a start within a year of today.");
        DigestRange range = Enum.IsDefined(request.Range) ? request.Range : Digests.DefaultRange;
        DateOnly? chosenEnd = EventDates.Parse(request.End);
        if (range == DigestRange.Custom && chosenEnd is null)
            throw Invalid("Choose the day the digest ends.");
        List<DigestEntry> entries = Checked(request.Entries);
        MediaRef? topPicture = request.TopPicture;
        await VerifyPicturesAsync(entries, cancellationToken);
        DigestRecord record = await RecordAsync(id, cancellationToken);
        record.Start = start;
        record.Range = range;
        record.End = Digests.EndOf(start, range, chosenEnd);
        record.CalendarIds = request.CalendarIds.Select(calendar => Guid.TryParse(calendar, out Guid calendarId) ? calendarId : Guid.Empty).Where(calendar => calendar != Guid.Empty).Distinct().ToList();
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        IReadOnlySet<string> showing = Digests.Candidates(book, record.Start, record.End, time.GetUtcNow().UtcDateTime)
            .Select(item => item.Key).ToHashSet();
        record.Entries = Digests.Kept(record.Entries, entries, showing);
        record.TopPicture = topPicture is null ? null : (await media.VerifyAsync([topPicture], cancellationToken))[0];
        await media.UseAsync(EventPictures.DigestOwner(record.Id), PicturesOf(record), cancellationToken);
        record.UpdatedAt = time.GetUtcNow().UtcDateTime;
        record.UpdatedBy = Caller.UserId(UserOf(context));
        await db.SaveChangesAsync(cancellationToken);
        return await DraftAsync(record, cancellationToken);
    }

    public async ValueTask<DigestDraft> SendToAppAsync(DigestRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid id = IdOf(request.Id);
        DigestRecord record = await db.Digests.SingleOrDefaultAsync(digest => digest.Id == id, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition, "Wait for the digest to save, then send it."));
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        IReadOnlyDictionary<Guid, HighlightedPost> posts = await PostsOfAsync(record.Entries.Select(entry => entry.Key), cancellationToken);
        SentDigest sent = Digests.ForApp(ItemsOf(book, posts, record), record.End);
        if (sent.Items.Count == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Tick at least one item to send."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        record.Sent = sent;
        record.SentAt = now;
        record.SentBy = Caller.UserId(UserOf(context));
        if (record.UpdatedAt == default)
            record.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        if (Digests.Notifies(record, request.NotifyAgain))
        {
            await notifications.RaiseOneOfAsync(Digests.ForEveryoneWhoSeesIt(book, posts, sent, record.Start).Select(each => each with { Actor = record.SentBy }).ToList(), cancellationToken);
            record.NotifiedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        return await DraftAsync(record, cancellationToken);
    }

    public async ValueTask<Done> SendToMyPhoneAsync(DigestRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        DigestRecord? latest = await LatestSentAsync(cancellationToken);
        if (latest is null || latest.Id != IdOf(request.Id))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Send the digest to the app first, so the notification has something to open."));
        if (Caller.UserId(UserOf(context)) is not { } me)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in to send the digest to your phone."));

        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        IReadOnlyDictionary<Guid, HighlightedPost> posts = await PostsOfAsync(latest.Sent.Items.Select(item => item.Item.Key), cancellationToken);
        await notifications.RaiseAsync(Digests.Notification(book, posts, latest.Sent, NotificationAudience.Person(me)) with { Test = true, Actor = me }, cancellationToken);
        return new Done();
    }

    private async Task<DigestDraft> DraftAsync(DigestRecord record, CancellationToken cancellationToken)
    {
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        IReadOnlyDictionary<Guid, HighlightedPost> posts = await PostsOfAsync(record.Entries.Select(entry => entry.Key), cancellationToken);
        List<DigestItem> items = ItemsOf(book, posts, record);
        IReadOnlyList<HighlightedPost> recent = await news.RecentAsync(RecentPosts, cancellationToken);
        DigestRecord? latest = await LatestSentAsync(cancellationToken);
        return new DigestDraft
        {
            Id = record.Id.ToString(),
            Start = EventDates.Format(record.Start),
            End = EventDates.Format(record.End),
            Range = record.Range,
            CalendarIds = CalendarsOf(book, record).Select(id => id.ToString()).ToList(),
            Calendars = book.Calendars.Values.OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name).Select(EventsPublisher.CalendarPayload).ToList(),
            Items = items,
            Calendar = Digests.Candidates(book, record.Start, record.End, time.GetUtcNow().UtcDateTime),
            Posts = recent.Select(Digests.NewsItem).ToList(),
            TopPicture = record.TopPicture,
            SavedAt = record.UpdatedAt != default ? record.UpdatedAt : null,
            SentAt = record.SentAt,
            NotifiedAt = record.NotifiedAt,
            SentAsItIs = record.SentAt is not null && Same(Digests.ForApp(items, record.End), record.Sent),
            LastSent = latest is { SentAt: { } at } ? new DigestSent { Start = EventDates.Format(latest.Start), SentAt = at } : null,
        };
    }

    private List<DigestItem> ItemsOf(EventsBook book, IReadOnlyDictionary<Guid, HighlightedPost> posts, DigestRecord record) =>
        Digests.Merge(Digests.Candidates(book, record.Start, record.End, time.GetUtcNow().UtcDateTime), posts, record.Entries);

    private Task<IReadOnlyDictionary<Guid, HighlightedPost>> PostsOfAsync(IEnumerable<string> keys, CancellationToken cancellationToken) =>
        news.PublishedAsync(Digests.PostsIn(keys), cancellationToken);

    private static List<MediaRef> PicturesOf(DigestRecord record) =>
        [.. new[] { record.TopPicture }.OfType<MediaRef>(), .. record.Entries.Where(entry => entry.Changed.Contains(DigestFields.Picture)).Select(entry => entry.Words.Picture).OfType<MediaRef>()];

    private async Task VerifyPicturesAsync(List<DigestEntry> entries, CancellationToken cancellationToken)
    {
        List<DigestEntry> pictured = entries.Where(entry => entry.Changed.Contains(DigestFields.Picture) && entry.Words.Picture is not null).ToList();
        if (pictured.Count == 0)
            return;
        IReadOnlyList<MediaRef> verified = await media.VerifyAsync(pictured.Select(entry => entry.Words.Picture!).ToList(), cancellationToken);
        for (int index = 0; index < pictured.Count; index++)
            pictured[index].Words = pictured[index].Words.WithPicture(verified[index]);
    }

    private static IReadOnlySet<Guid> CalendarsOf(EventsBook book, DigestRecord record) =>
        record.CalendarIds is { } chosen ? chosen.Where(book.Calendars.ContainsKey).ToHashSet() : Digests.DefaultCalendars(book);

    private Task<DigestRecord?> CurrentAsync(CancellationToken cancellationToken) =>
        db.Digests.AsNoTracking().Where(Digests.Current(ChurchTime.Today(time)))
            .OrderByDescending(digest => digest.UpdatedAt).FirstOrDefaultAsync(cancellationToken);

    private Task<DigestRecord?> LatestSentAsync(CancellationToken cancellationToken) =>
        db.Digests.AsNoTracking().Where(digest => digest.SentAt != null).OrderByDescending(digest => digest.SentAt).FirstOrDefaultAsync(cancellationToken);

    private DigestRecord Fresh()
    {
        DateOnly today = ChurchTime.Today(time);
        return new DigestRecord { Id = Guid.CreateVersion7(), Start = today, Range = Digests.DefaultRange, End = Digests.EndOf(today, Digests.DefaultRange) };
    }

    private async Task<DigestRecord> RecordAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.Digests.SingleOrDefaultAsync(digest => digest.Id == id, cancellationToken) is { } existing)
            return existing;
        DigestRecord created = new() { Id = id };
        db.Digests.Add(created);
        return created;
    }

    private static Guid IdOf(string text) =>
        Guid.TryParse(text, out Guid id) && id != Guid.Empty ? id : throw Invalid("Something went wrong with the digest. Open it again and try once more.");

    private static List<DigestEntry> Checked(IReadOnlyList<DigestEntry> entries)
    {
        if (entries.Count > MostEntries)
            throw Invalid($"A digest can hold up to {MostEntries} items.");
        if (entries.Select(entry => entry.Key).Distinct().Count() != entries.Count || entries.Any(entry => entry.Key.Length is 0 or > LongestWords))
            throw Invalid("Something went wrong saving the digest. Open it again and try once more.");
        foreach (DigestEntry entry in entries)
        {
            if (entry.Own != entry.Key.StartsWith(Digests.OwnPrefix, StringComparison.Ordinal))
                throw Invalid("Something went wrong saving the digest. Open it again and try once more.");
            DigestWords words = entry.Words;
            if (new[] { words.Label, words.Title, words.When, words.Time, words.RegisterLink }.Any(text => text.Length > LongestWords) || words.Description.Length > LongestDescription)
                throw Invalid("Some of the wording is too long for the digest.");
            if (words.RegisterLink.Length > 0 && !(Uri.TryCreate(words.RegisterLink, UriKind.Absolute, out Uri? link) && link.Scheme is "https" or "http"))
                throw Invalid("The registration link needs to be a web address starting with https://.");
            if (entry.Own && words.Title.Trim().Length == 0)
                throw Invalid("Enter a title for the item you added.");
        }
        return entries.Select(entry => new DigestEntry
        {
            Key = entry.Key, Included = entry.Included, Own = entry.Own, Words = entry.Words.Copy(), PictureAt = Enum.IsDefined(entry.PictureAt) ? entry.PictureAt : DigestPictureAt.Beside,
            Changed = entry.Own ? [.. DigestFields.All] : entry.Changed.Where(DigestFields.All.Contains).Distinct().ToList(),
        }).ToList();
    }

    private static bool Same(SentDigest first, SentDigest second) =>
        JsonSerializer.Serialize(first, JsonSerializerOptions.Web) == JsonSerializer.Serialize(second, JsonSerializerOptions.Web);

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}
