using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public sealed class PrayerOnPhone(ILocalFeeds feeds, IOutbox outbox, ILocalPreferences preferences, IAccess access, TimeProvider time)
{
    public static readonly IReadOnlyList<string> Feeds = [PrayerFeeds.Wall, PrayerFeeds.Mine];

    public const string Service = "/gsbc.prayer.Prayer/";
    private const string KeptKey = "prayer.kept";
    private const int Page = 200;
    private static readonly TimeSpan KeptFor = TimeSpan.FromDays(365);
    private readonly SemaphoreSlim _keeping = new(1, 1);

    public bool CanSend => outbox.IsAvailable;

    public static string Describe(string method) => method[Service.Length..] switch
    {
        "Send" => "your prayer request",
        "Pray" => "your prayer",
        "Answered" => "that it was answered",
        "TakeDown" => "taking it off the prayer wall",
        "Delete" => "deleting your request",
        _ => "a change",
    };

    public async Task<PrayerView> LoadAsync()
    {
        if (!feeds.IsAvailable)
            return PrayerView.Empty;

        List<WallPost> wall = await ReadAllAsync<WallPost>(PrayerFeeds.Wall);
        List<YourRequest> mine = access.SignedIn ? await ReadAllAsync<YourRequest>(PrayerFeeds.Mine) : [];
        Dictionary<string, OutboxEntry> waiting = (await outbox.ListAsync()).ToDictionary(entry => entry.Id);
        KeptOnPhone kept = await KeptAsync();

        Dictionary<string, WallPost> onWall = wall.ToDictionary(post => post.Id);
        HashSet<string> mineIds = mine.Select(request => request.Id).ToHashSet();
        List<YourLine> yours = mine.Select(request => FromFeed(request, onWall.GetValueOrDefault(request.Id))).ToList();
        yours.AddRange(kept.Sent
            .Where(sent => !mineIds.Contains(sent.Id) && (access.SignedIn || !sent.SignedIn))
            .Select(sent => FromPhone(sent, waiting.GetValueOrDefault(sent.OutboxId), onWall.GetValueOrDefault(sent.Id))));

        HashSet<string> yourIds = yours.Select(line => line.Id).ToHashSet();
        HashSet<string> prayedHere = kept.Prayed.Select(prayed => prayed.Id).ToHashSet();
        return new PrayerView(
            yours.OrderByDescending(line => line.SentAt).ToList(),
            wall.Select(post => new WallLine(post, prayedHere.Contains(post.Id), yourIds.Contains(post.Id))).ToList());
    }

    public async Task<string> SendAsync(PrayerAsk ask)
    {
        PrayerRequestSend send = new()
        {
            Id = Guid.NewGuid().ToString(),
            Text = ask.Text.Trim(),
            ForWall = ask.ForWall,
            ShowName = ask.ForWall && ask.ShowName,
            Name = access.SignedIn ? "" : ask.Name.Trim(),
            WantsContact = ask.WantsContact,
            Contact = ask.WantsContact ? ask.Contact.Trim() : "",
        };
        string outboxId = await outbox.EnqueueAsync(Service + "Send", send, PrayerContractsModel.Instance);
        await KeepAsync(kept => kept.Sent.Add(new SentOnPhone
        {
            Id = send.Id, Text = send.Text, SentAt = time.GetUtcNow().UtcDateTime, ForWall = send.ForWall, OutboxId = outboxId, SignedIn = access.SignedIn,
        }));
        return send.Id;
    }

    public async Task PrayAsync(string id, bool prayed)
    {
        string outboxId = await outbox.EnqueueAsync(Service + "Pray", new PrayMark { Id = id, Prayed = prayed }, PrayerContractsModel.Instance);
        await KeepAsync(kept =>
        {
            kept.Prayed.RemoveAll(mark => mark.Id == id);
            if (prayed)
                kept.Prayed.Add(new PrayedOnPhone { Id = id, OutboxId = outboxId });
        });
    }

    public async Task AnsweredAsync(string id, string note) =>
        await outbox.EnqueueAsync(Service + "Answered", new AnsweredSave { Id = id, Note = note.Trim() }, PrayerContractsModel.Instance);

    public async Task TakeDownAsync(string id) =>
        await outbox.EnqueueAsync(Service + "TakeDown", new RequestRef { Id = id }, PrayerContractsModel.Instance);

    public async Task DeleteAsync(string id)
    {
        if ((await KeptAsync()).Sent.FirstOrDefault(sent => sent.Id == id) is { } sent
            && (await outbox.ListAsync()).Any(entry => entry.Id == sent.OutboxId))
            await outbox.DiscardAsync(sent.OutboxId);
        await outbox.EnqueueAsync(Service + "Delete", new RequestRef { Id = id }, PrayerContractsModel.Instance);
        await KeepAsync(kept => kept.Sent.RemoveAll(entry => entry.Id == id));
    }

    public async Task RetryAsync(string id)
    {
        if ((await KeptAsync()).Sent.FirstOrDefault(sent => sent.Id == id) is { } sent)
            await outbox.RetryAsync(sent.OutboxId);
    }

    private YourLine FromFeed(YourRequest request, WallPost? post) => new(
        request.Id, request.Text, request.SentAt, Sending.Sent, null, request.Wall, request.WallUntil, post?.Prayed ?? request.Prayed,
        request.Answered, request.AnsweredNote, request.WallName, request.Wall != WallState.NotAsked, Known: true);

    private static YourLine FromPhone(SentOnPhone sent, OutboxEntry? entry, WallPost? post) => new(
        sent.Id, sent.Text, sent.SentAt,
        entry is null ? Sending.Sent : entry.Failed ? Sending.CouldNotSend : Sending.Waiting,
        entry?.Failed == true ? entry.LastError : null,
        post is not null ? WallState.Shown : sent.ForWall ? WallState.Waiting : WallState.NotAsked,
        post?.Until, post?.Prayed ?? 0, post?.Answered ?? false, post?.AnsweredNote ?? "", post?.Name ?? "", sent.ForWall, Known: false);

    private async Task<KeptOnPhone> KeptAsync()
    {
        string? stored = await preferences.GetAsync(KeptKey);
        if (string.IsNullOrEmpty(stored))
            return new KeptOnPhone();
        using MemoryStream stream = new(Convert.FromBase64String(stored));
        KeptOnPhone kept = PrayerContractsModel.Instance.Deserialize<KeptOnPhone>(stream);
        DateTime oldest = time.GetUtcNow().UtcDateTime - KeptFor;
        kept.Sent.RemoveAll(sent => sent.SentAt < oldest);
        return kept;
    }

    private async Task KeepAsync(Action<KeptOnPhone> change)
    {
        await _keeping.WaitAsync();
        try
        {
            KeptOnPhone kept = await KeptAsync();
            change(kept);
            using MemoryStream stream = new();
            PrayerContractsModel.Instance.Serialize(stream, kept);
            await preferences.SetAsync(KeptKey, Convert.ToBase64String(stream.ToArray()));
        }
        finally
        {
            _keeping.Release();
        }
    }

    private async Task<List<T>> ReadAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string feed)
    {
        List<T> all = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, Descending: true, Limit: Page, Offset: offset));
            all.AddRange(page.Select(record => PrayerContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload))));
            if (page.Count < Page)
                return all;
        }
    }
}
