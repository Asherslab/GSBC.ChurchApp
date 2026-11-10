using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerPublisher(PrayerDbContext db)
{
    public void Changed(RequestRecord request, DateTime now)
    {
        Publish(request, now);
        db.FeedChanges.Add(FeedChangeRecord.Marker(PrayerFeeds.Inbox, request.Id.ToString(), now));
    }

    public void Publish(RequestRecord request, DateTime now)
    {
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
        if (request.WallShownAt is not null)
            db.FeedChanges.Add(Gone(PrayerFeeds.Wall, request.Id, now));
        if (request.UserId is { } owner)
            db.FeedChanges.Add(new FeedChangeRecord
                { Feed = PrayerFeeds.Mine, RecordId = request.Id.ToString(), ScopeId = owner.ToString(), Version = now.Ticks, Deleted = true });
        db.FeedChanges.Add(FeedChangeRecord.Marker(PrayerFeeds.Inbox, request.Id.ToString(), now, deleted: true));
    }

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
