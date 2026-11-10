using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Notifications;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed class SermonFollows(ILocalFeeds feeds, IAccess access, IOutbox outbox, ILocalPreferences preferences, ClientSignals signals)
    : IFollowedScopes, IClientStartupTask, IDisposable
{
    private const string OnThisPhone = "sermons.follows";
    private const string FollowMethod = "/gsbc.sermons.Sermons/Follow";
    private HashSet<string> _scopes = [];

    public event Action? Changed;

    public IReadOnlySet<string> Scopes => _scopes;

    public bool NewSermons => _scopes.Contains(SermonsNotifications.AllSermonsScope);

    public bool Series(string seriesId) => _scopes.Contains(seriesId);

    public async Task RunAsync()
    {
        signals.Changed += OnSignals;
        await LoadAsync();
    }

    public async Task<string?> SetAsync(string? seriesId, bool follow)
    {
        string scope = seriesId ?? SermonsNotifications.AllSermonsScope;
        HashSet<string> scopes = [.. _scopes];
        if (follow)
            scopes.Add(scope);
        else
            scopes.Remove(scope);
        await outbox.EnqueueAsync(FollowMethod, new FollowRequest { SeriesId = seriesId, Follow = follow }, SermonsContractsModel.Instance);
        if (!access.SignedIn)
            await preferences.SetAsync(OnThisPhone, string.Join(',', scopes));
        Take(scopes);
        return null;
    }

    public void Dispose() => signals.Changed -= OnSignals;

    private async Task LoadAsync()
    {
        if (!access.SignedIn)
        {
            Take((await preferences.GetAsync(OnThisPhone) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet());
            return;
        }
        IReadOnlyList<StoredRecord> mine = await feeds.ReadPageAsync(new FeedPageQuery(SermonsFeeds.Follows, Limit: 1));
        Follows follows = mine.Count == 0 ? new Follows() : SermonsContractsModel.Instance.Deserialize<Follows>(new MemoryStream(mine[0].Payload));
        HashSet<string> scopes = [.. follows.SeriesIds];
        if (follows.NewSermons)
            scopes.Add(SermonsNotifications.AllSermonsScope);
        Take(scopes);
    }

    private void Take(HashSet<string> scopes)
    {
        _scopes = scopes;
        Changed?.Invoke();
    }

    private void OnSignals(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Session | ChangeKind.Feeds, [SermonsFeeds.Follows]))
            _ = LoadAsync();
    }
}
