using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Notifications;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public sealed record InboxEntry(InboxItem Item, bool Unread);

public sealed class Inbox : IInboxCount, IDisposable
{
    private const string Route = "notifications";
    private const string MarkReadMethod = "/gsbc.notifications.Notifications/MarkRead";
    public static readonly IReadOnlyList<string> Feeds = [NotificationsFeeds.Inbox, NotificationsFeeds.Public];

    public const string Remembered = "notifications.inbox";
    private const int MostShown = 100;
    private const string ReadOnThisPhone = "notifications.read";

    private readonly ILocalFeeds _feeds;
    private readonly IAccess _access;
    private readonly IOutbox _outbox;
    private readonly ILocalPreferences _preferences;
    private readonly NativeBridge _bridge;
    private readonly ClientSignals _signals;
    private readonly IEnumerable<IFollowedScopes> _followed;
    private readonly NotificationPreferences _pushes;
    private readonly HashSet<string> _readHere = [];

    public Inbox(ILocalFeeds feeds, IAccess access, IOutbox outbox, ILocalPreferences preferences, NativeBridge bridge, ClientSignals signals,
        IEnumerable<IFollowedScopes> followed, NotificationPreferences pushes)
    {
        _followed = followed;
        _pushes = pushes;
        _feeds = feeds;
        _access = access;
        _outbox = outbox;
        _preferences = preferences;
        _bridge = bridge;
        _signals = signals;
        _signals.Changed += OnChanged;
    }

    public int Unread { get; private set; }

    public int Waiting { get; private set; }

    public string Href => Route;

    public event Action? Changed;

    public async Task<IReadOnlyList<InboxEntry>> LoadAsync()
    {
        List<InboxEntry> entries;
        if (_access.SignedIn)
        {
            entries = (await ReadAsync(NotificationsFeeds.Inbox)).Select(item => new InboxEntry(item, item.ReadAt is null)).ToList();
        }
        else
        {
            await LoadReadHereAsync();
            List<InboxItem> followed = (await ReadAsync(NotificationsFeeds.Public))
                .Where(item => item.ScopeId is null || _followed.Any(follows => follows.Scopes.Contains(item.ScopeId)))
                .ToList();
            entries = OneNotification.OnePerThing(followed, await _pushes.PushesOnThisPhoneAsync())
                .Select(item => new InboxEntry(item, !_readHere.Contains(item.Id)))
                .ToList();
        }

        await CountAsync(entries.Count(entry => entry.Unread), entries.Count(entry => entry.Item.Waiting));
        return entries;
    }

    public async Task MarkReadAsync(IReadOnlyCollection<InboxEntry> entries)
    {
        List<InboxEntry> unread = entries.Where(entry => entry.Unread).ToList();
        if (unread.Count == 0)
            return;

        if (_access.SignedIn)
        {
            await _outbox.EnqueueAsync(MarkReadMethod, new MarkReadRequest { Ids = unread.Select(entry => entry.Item.Id).ToList() }, NotificationsContractsModel.Instance);
        }
        else
        {
            await LoadReadHereAsync();
            _readHere.UnionWith(unread.Select(entry => entry.Item.Id));
            await _preferences.SetAsync(ReadOnThisPhone, string.Join(',', _readHere.TakeLast(MostShown * 2)));
        }

        await CountAsync(Math.Max(0, Unread - unread.Count), Waiting);
    }

    public Task RefreshCountAsync() => LoadAsync();

    public void Dispose() => _signals.Changed -= OnChanged;

    private async Task<List<InboxItem>> ReadAsync(string feed) =>
        (await _feeds.ReadPageAsync(new FeedPageQuery(feed, Descending: true, Limit: MostShown)))
            .Select(record => NotificationsContractsModel.Instance.Deserialize<InboxItem>(new MemoryStream(record.Payload)))
            .ToList();

    private async Task LoadReadHereAsync()
    {
        if (_readHere.Count > 0)
            return;
        if (await _preferences.GetAsync(ReadOnThisPhone) is { Length: > 0 } stored)
            _readHere.UnionWith(stored.Split(','));
    }

    private async Task CountAsync(int unread, int waiting)
    {
        bool changed = unread != Unread || waiting != Waiting;
        Unread = unread;
        Waiting = waiting;
        await _bridge.SetBadgeAsync(unread);
        if (changed)
            Changed?.Invoke();
    }

    private void OnChanged(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Feeds | ChangeKind.Session, Feeds))
            _ = LoadAsync();
    }
}
