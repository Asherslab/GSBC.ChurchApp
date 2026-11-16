using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PastoralCare(
    PrayerDbContext db, PrayerPublisher publisher, PrayerTelling telling, HandledRequests handled, IPeopleIndex people, IFamilies families,
    ChurchName church, TimeProvider time)
{
    public async Task WriteAsync(Guid staff, PastoralWrite write, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(write.Id, out Guid id) || id == Guid.Empty)
            throw Invalid("A prayer request needs its id.");
        if (await db.Requests.AnyAsync(request => request.Id == id, cancellationToken))
            return;
        string text = write.Text.Trim();
        if (text.Length == 0 || text.Length > PrayerRules.LongestText)
            throw Invalid($"Write a request of up to {PrayerRules.LongestText} characters.");
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime? until = write.ForWall
            ? PrayerRules.ClampedUntil(DateTime.SpecifyKind(write.Until, DateTimeKind.Utc), now) ?? throw Invalid("Choose a day within the next year for the request to leave the wall.")
            : null;

        bool theirOwn = write.From is WrittenFrom.Me or WrittenFrom.NoName;
        RequestRecord record = new()
        {
            Id = id,
            Text = text,
            SentAt = now,
            ChangedAt = now,
            UserId = theirOwn ? staff : null,
            Name = await NameAsync(staff, write, cancellationToken),
            Minor = write.From == WrittenFrom.Me && (await families.FactsAsync([staff], cancellationToken)).TryGetValue(staff, out FamilyFacts? facts) && facts.Minor,
            ShowName = write.From != WrittenFrom.NoName,
            FromChurch = write.From == WrittenFrom.Church,
            ForSomeoneElse = write.From == WrittenFrom.SomeoneElse,
            Wall = write.ForWall ? WallState.Shown : WallState.NotAsked,
            WallText = text,
            WallShowsName = write.From != WrittenFrom.NoName,
            WallShownAt = write.ForWall ? now : null,
            WallUntil = until,
        };
        record.Steps.Add(new StepRecord { WhoId = staff, At = now, What = "Wrote it in the admin panel" });
        if (until is { } leaves)
            record.Steps.Add(new StepRecord { WhoId = staff, At = now, What = $"Put it on the prayer wall, until {ChurchTime.ToChurch(leaves):d MMMM}" });
        db.Requests.Add(record);
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (!record.FromChurch)
            await telling.NewRequestAsync(record, cancellationToken);
    }

    private async Task<string> NameAsync(Guid staff, PastoralWrite write, CancellationToken cancellationToken) => write.From switch
    {
        WrittenFrom.Me => (await people.NamesAsync([staff], cancellationToken)).GetValueOrDefault(staff, ""),
        WrittenFrom.SomeoneElse => PrayerRules.Clip(write.Name, PrayerRules.LongestName) is { Length: > 0 } name ? name : throw Invalid("Type the name of who asked for prayer."),
        WrittenFrom.Church => await church.CurrentAsync(cancellationToken),
        _ => "",
    };

    public async Task<Inbox> InboxAsync(bool mayRead, string search, CancellationToken cancellationToken)
    {
        IQueryable<RequestRecord> requests = db.Requests.AsNoTracking();
        if (!mayRead)
            requests = requests.Where(request => request.Wall != WallState.NotAsked);
        string text = search.Trim();
        if (text.Length > 0)
            requests = requests.Where(request => EF.Functions.ILike(request.Text, $"%{text}%") || EF.Functions.ILike(request.Name, $"%{text}%"));
        List<RequestRecord> found = await requests.OrderByDescending(request => request.SentAt).ToListAsync(cancellationToken);
        return new Inbox { Requests = found.Select(PrayerRules.Line).ToList(), MayRead = mayRead };
    }

    public async Task<RequestDetails> RequestAsync(bool mayRead, string id, CancellationToken cancellationToken)
    {
        Guid requestId = IdOf(id);
        RequestRecord record = await db.Requests.AsNoTracking().Include(request => request.Notes).Include(request => request.Steps)
            .FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken) ?? throw NotFound();
        if (!mayRead && !PrayerRules.MayModerate(record))
            throw NotFound();

        RequestDetails details = new()
        {
            Line = PrayerRules.Line(record),
            SignedIn = record.UserId is not null,
            ShowName = record.ShowName,
            ShortName = PrayerRules.WallName(record),
            WallText = record.WallText,
            WallName = record.WallShowsName ? PrayerRules.WallName(record) : "",
            WallUntil = record.WallUntil,
            Prayed = record.Prayed,
            AnsweredNote = record.AnsweredNote,
            MayRead = mayRead,
            MayDelete = PrayerRules.MayDelete(record, mayRead),
            FromChurch = record.FromChurch,
            ForSomeoneElse = record.ForSomeoneElse,
        };
        if (!mayRead)
            return details;

        HashSet<Guid> named = record.Notes.Select(note => note.AuthorId).Concat(record.Steps.Select(step => step.WhoId).OfType<Guid>()).ToHashSet();
        IReadOnlyDictionary<Guid, string> names = named.Count == 0 ? new Dictionary<Guid, string>() : await people.NamesAsync(named, cancellationToken);
        details.Notes = record.Notes.OrderBy(note => note.At)
            .Select(note => new CareNote { Id = note.Id.ToString(), Text = note.Text, Author = names.GetValueOrDefault(note.AuthorId, "Someone"), At = note.At })
            .ToList();
        details.History = record.Steps.OrderBy(step => step.At).ThenBy(step => step.Id)
            .Select(step => new CareStep
            {
                What = step.What,
                Who = step.WhoId is { } who ? names.GetValueOrDefault(who, "Someone") : PrayerRules.From(record),
                At = step.At,
            })
            .ToList();
        return details;
    }

    public async Task CareAsync(Guid staff, CareChange change, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        RequestRecord record = await FindAsync(change.Id, cancellationToken);
        if (record.Care == change.Care && record.AnsweredNote == change.AnsweredNote.Trim())
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        bool newlyAnswered = change.Care == CareState.Answered && record.Care != CareState.Answered;
        record.Care = change.Care;
        if (change.Care == CareState.Answered)
            record.AnsweredNote = PrayerRules.Clip(change.AnsweredNote, PrayerRules.LongestAnsweredNote);
        Step(record, staff, now, change.Care == CareState.Answered ? "Marked answered" : "Marked new again");
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (change.Care != CareState.New)
            await telling.CaredForAsync(record, cancellationToken);
        if (newlyAnswered)
            await telling.AnsweredAsync(record, cancellationToken);
    }

    public async Task AddNoteAsync(Guid staff, NoteSave save, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        string text = save.Text.Trim();
        if (text.Length == 0 || text.Length > PrayerRules.LongestNote)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Write a note of up to {PrayerRules.LongestNote} characters."));
        RequestRecord record = await FindAsync(save.RequestId, cancellationToken);
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Notes.Add(new NoteRecord { Id = Guid.NewGuid(), RequestId = record.Id, AuthorId = staff, Text = text, At = now });
        Step(record, staff, now, "Added a note");
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteNoteAsync(Guid staff, NoteRef note, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        RequestRecord record = await FindAsync(note.RequestId, cancellationToken);
        if (!Guid.TryParse(note.NoteId, out Guid noteId)
            || await db.Notes.FirstOrDefaultAsync(row => row.Id == noteId && row.RequestId == record.Id, cancellationToken) is not { } found)
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Notes.Remove(found);
        Step(record, staff, now, "Deleted a note");
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ShowOnWallAsync(Guid staff, WallSave save, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        RequestRecord record = await FindAsync(save.Id, cancellationToken);
        if (!PrayerRules.MayModerate(record))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "They didn't ask for the prayer wall, so the request stays with the pastoral team."));
        if (!PrayerRules.MayGoOnTheWall(record))
            return;
        string text = save.Text.Trim();
        if (text.Length == 0 || text.Length > PrayerRules.LongestText)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Keep the words on the wall to {PrayerRules.LongestText} characters or fewer."));
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime until = PrayerRules.ClampedUntil(DateTime.SpecifyKind(save.Until, DateTimeKind.Utc), now)
            ?? throw new RpcException(new Status(StatusCode.InvalidArgument, "Choose a day within the next year for the request to leave the wall."));

        bool wasShown = PrayerRules.IsOnTheWall(record, now);
        record.Wall = WallState.Shown;
        record.WallText = text;
        record.WallShowsName = PrayerRules.ShowsNameOnTheWall(record, save.ShowName);
        record.WallUntil = until;
        record.WallShownAt ??= now;
        Step(record, staff, now, $"{(wasShown ? "Changed it on the prayer wall" : "Put it on the prayer wall")}, until {ChurchTime.ToChurch(until):d MMMM}");
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.WallDecidedAsync(record, cancellationToken);
    }

    public async Task KeepPrivateAsync(Guid staff, RequestRef request, string? queuedId, CancellationToken cancellationToken)
    {
        if (await handled.AlreadyAsync(queuedId, cancellationToken))
            return;
        RequestRecord record = await FindAsync(request.Id, cancellationToken);
        if (record.Wall is not (WallState.Waiting or WallState.Shown))
            return;
        DateTime now = time.GetUtcNow().UtcDateTime;
        Step(record, staff, now, record.Wall == WallState.Shown ? "Took it off the prayer wall" : "Kept it off the prayer wall");
        record.Wall = WallState.KeptPrivate;
        await publisher.ChangedAsync(record, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.WallDecidedAsync(record, cancellationToken);
    }

    public async Task DeleteAsync(bool mayRead, RequestRef request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out Guid id) || await db.Requests.FirstOrDefaultAsync(row => row.Id == id, cancellationToken) is not { } record)
            return;
        if (!PrayerRules.MayDelete(record, mayRead))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only the pastoral team can delete a request someone sent."));
        db.Requests.Remove(record);
        publisher.Deleted(record, time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        await telling.DeletedAsync(record, cancellationToken);
    }

    private async Task<RequestRecord> FindAsync(string id, CancellationToken cancellationToken)
    {
        Guid requestId = IdOf(id);
        return await db.Requests.FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken) ?? throw NotFound();
    }

    private void Step(RequestRecord record, Guid staff, DateTime now, string what)
    {
        record.ChangedAt = now;
        db.Steps.Add(new StepRecord { RequestId = record.Id, WhoId = staff, At = now, What = what });
    }

    private static Guid IdOf(string id) => Guid.TryParse(id, out Guid parsed) ? parsed : throw NotFound();

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException NotFound() => new(new Status(StatusCode.NotFound, "That prayer request isn't here any more."));
}
