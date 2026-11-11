using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerPublisher(PrayerDbContext db, IPeopleIndex people)
{
    public async Task ChangedAsync(RequestRecord request, DateTime now, CancellationToken cancellationToken)
    {
        await PublishAsync(request, now, cancellationToken);
        db.FeedChanges.Add(FeedChangeRecord.Marker(PrayerFeeds.Inbox, request.Id.ToString(), now));
    }

    public async Task PublishAsync(RequestRecord request, DateTime now, CancellationToken cancellationToken)
    {
        await ForStaffAsync(request, now, cancellationToken);

        if (PrayerRules.IsOnTheWall(request, now))
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = PrayerFeeds.Wall,
                RecordId = request.Id.ToString(),
                Version = now.Ticks,
                SortKey = (request.WallShownAt ?? request.SentAt).ToString("O"),
                StartsAt = request.WallShownAt,
                EndsAt = request.WallUntil,
                Payload = PrayerPayloads.Encode(PrayerRules.Post(request)),
            });
        else if (request.WallShownAt is not null)
            db.FeedChanges.Add(Gone(PrayerFeeds.Wall, request.Id, now));

        if (request.UserId is { } owner)
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = PrayerFeeds.Mine,
                RecordId = request.Id.ToString(),
                ScopeId = owner.ToString(),
                Version = now.Ticks,
                SortKey = request.SentAt.ToString("O"),
                Payload = PrayerPayloads.Encode(PrayerRules.Yours(request)),
            });
    }

    public void Deleted(RequestRecord request, DateTime now)
    {
        if (PrayerRules.MayModerate(request))
            db.FeedChanges.Add(Gone(PrayerFeeds.Approving, request.Id, now));
        db.FeedChanges.Add(Gone(PrayerFeeds.Care, request.Id, now));
        if (request.WallShownAt is not null)
            db.FeedChanges.Add(Gone(PrayerFeeds.Wall, request.Id, now));
        if (request.UserId is { } owner)
            db.FeedChanges.Add(new FeedChangeRecord
                { Feed = PrayerFeeds.Mine, RecordId = request.Id.ToString(), ScopeId = owner.ToString(), Version = now.Ticks, Deleted = true });
        db.FeedChanges.Add(FeedChangeRecord.Marker(PrayerFeeds.Inbox, request.Id.ToString(), now, deleted: true));
    }

    private async Task ForStaffAsync(RequestRecord request, DateTime now, CancellationToken cancellationToken)
    {
        if (PrayerRules.IsForApprovers(request))
            db.FeedChanges.Add(Staff(PrayerFeeds.Approving, request, now, PrayerRules.ForApprovers(request)));
        else if (PrayerRules.MayModerate(request))
            db.FeedChanges.Add(Gone(PrayerFeeds.Approving, request.Id, now));

        if (!PrayerRules.IsForThePastoralTeamsPhones(request))
        {
            db.FeedChanges.Add(Gone(PrayerFeeds.Care, request.Id, now));
            return;
        }
        await db.Notes.Where(note => note.RequestId == request.Id).LoadAsync(cancellationToken);
        List<NoteRecord> notes = db.Notes.Local.Where(note => note.RequestId == request.Id).OrderBy(note => note.At).ToList();
        IReadOnlyDictionary<Guid, string> names = notes.Count == 0
            ? new Dictionary<Guid, string>()
            : await people.NamesAsync(notes.Select(note => note.AuthorId).ToHashSet(), cancellationToken);
        db.FeedChanges.Add(Staff(PrayerFeeds.Care, request, now, PrayerRules.ForThePastoralTeam(request, notes, names)));
    }

    private static FeedChangeRecord Staff<T>(string feed, RequestRecord request, DateTime now, T payload) => new()
    {
        Feed = feed,
        RecordId = request.Id.ToString(),
        Version = now.Ticks,
        SortKey = request.SentAt.ToString("O"),
        Payload = PrayerPayloads.Encode(payload),
    };

    private static FeedChangeRecord Gone(string feed, Guid id, DateTime now) =>
        new() { Feed = feed, RecordId = id.ToString(), Version = now.Ticks, Deleted = true };
}

internal static class PrayerPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        PrayerContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return PrayerContractsModel.Instance.Deserialize<T>(stream);
    }
}
