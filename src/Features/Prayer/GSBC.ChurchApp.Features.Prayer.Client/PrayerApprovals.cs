using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public sealed class PrayerApprovals(ILocalFeeds feeds, IOutbox outbox, ILocalPreferences preferences, IAccess access)
{
    public static readonly IReadOnlyList<string> Feeds = [PrayerFeeds.Approving, PrayerFeeds.Care];

    private const string Service = "/gsbc.prayer.Pastoral/";
    private const string QueuedKey = "prayer.staff.queued";
    private const int Page = 200;
    private readonly SemaphoreSlim _queueing = new(1, 1);

    public bool MayApprove => access.SignedIn && access.Can(PrayerPermissions.Wall);

    public bool MayCare => access.SignedIn && access.Can(PrayerPermissions.Read);

    public async Task<StaffView> LoadAsync()
    {
        if (!feeds.IsAvailable || !MayApprove)
            return StaffView.Empty;

        Dictionary<string, WallCase> wall = (await ReadAllAsync<WallCase>(PrayerFeeds.Approving)).ToDictionary(item => item.Id);
        Dictionary<string, CareCase> care = MayCare ? (await ReadAllAsync<CareCase>(PrayerFeeds.Care)).ToDictionary(item => item.Id) : [];
        Dictionary<string, OutboxEntry> entries = (await outbox.ListAsync()).ToDictionary(entry => entry.Id);
        List<QueuedOnPhone> queued = await PruneAsync(entries);

        return new StaffView(wall.Keys.Union(care.Keys)
            .Select(id => new StaffCase(id, wall.GetValueOrDefault(id), care.GetValueOrDefault(id), Pending(id, queued, entries)))
            .OrderByDescending(item => item.SentAt)
            .ToList(), MayCare);
    }

    public Task ShowAsync(WallSave save, bool change) =>
        QueueAsync(save.Id, "ShowOnWall", save, change ? "Changing it on the prayer wall" : "Putting it on the prayer wall");

    public Task KeepPrivateAsync(string id, bool shown) =>
        QueueAsync(id, "KeepPrivate", new RequestRef { Id = id }, shown ? "Taking it off the prayer wall" : "Keeping it off the prayer wall");

    public Task CareAsync(string id, CareState care, string answeredNote = "") =>
        QueueAsync(id, "Care", new CareChange { Id = id, Care = care, AnsweredNote = answeredNote.Trim() },
            care switch
            {
                CareState.Answered => "Marking it answered",
                CareState.FollowedUp => "Marking it followed up",
                _ => "Marking it new again",
            });

    public Task AddNoteAsync(string id, string text) =>
        QueueAsync(id, "AddNote", new NoteSave { RequestId = id, Text = text.Trim() }, "Adding your note");

    public async Task RetryAsync(string id)
    {
        foreach (QueuedOnPhone queued in (await QueuedAsync()).Where(item => item.RequestId == id))
            await outbox.RetryAsync(queued.OutboxId);
    }

    private static StaffPending? Pending(string id, List<QueuedOnPhone> queued, Dictionary<string, OutboxEntry> entries)
    {
        List<(QueuedOnPhone Queued, OutboxEntry Entry)> waiting = queued.Where(item => item.RequestId == id && entries.ContainsKey(item.OutboxId))
            .Select(item => (item, entries[item.OutboxId])).ToList();
        if (waiting.Count == 0)
            return null;
        (QueuedOnPhone last, OutboxEntry lastEntry) = waiting[^1];
        OutboxEntry? failed = waiting.Select(item => item.Entry).FirstOrDefault(entry => entry.Failed);
        return new StaffPending(last.What, failed is not null, failed?.LastError ?? (lastEntry.Failed ? lastEntry.LastError : null));
    }

    private async Task QueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string id, string method, T request, string what)
    {
        string outboxId = await outbox.EnqueueAsync(Service + method, request, PrayerContractsModel.Instance);
        await _queueing.WaitAsync();
        try
        {
            List<QueuedOnPhone> queued = await QueuedAsync();
            queued.Add(new QueuedOnPhone { RequestId = id, OutboxId = outboxId, What = what });
            await RememberAsync(queued);
        }
        finally
        {
            _queueing.Release();
        }
    }

    private async Task<List<QueuedOnPhone>> PruneAsync(Dictionary<string, OutboxEntry> entries)
    {
        await _queueing.WaitAsync();
        try
        {
            List<QueuedOnPhone> queued = await QueuedAsync();
            if (queued.RemoveAll(item => !entries.ContainsKey(item.OutboxId)) > 0)
                await RememberAsync(queued);
            return queued;
        }
        finally
        {
            _queueing.Release();
        }
    }

    private async Task<List<QueuedOnPhone>> QueuedAsync()
    {
        string? stored = await preferences.GetAsync(QueuedKey);
        if (string.IsNullOrEmpty(stored))
            return [];
        using MemoryStream stream = new(Convert.FromBase64String(stored));
        return PrayerContractsModel.Instance.Deserialize<StaffQueue>(stream).Queued;
    }

    private async Task RememberAsync(List<QueuedOnPhone> queued)
    {
        using MemoryStream stream = new();
        PrayerContractsModel.Instance.Serialize(stream, new StaffQueue { Queued = queued });
        await preferences.SetAsync(QueuedKey, queued.Count == 0 ? null : Convert.ToBase64String(stream.ToArray()));
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
