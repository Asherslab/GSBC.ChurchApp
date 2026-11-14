using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Files;
using GSBC.ChurchApp.Platform.Client.Location;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Permissions;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

internal sealed class StoredFeeds : ILocalFeeds
{
    private readonly Dictionary<string, List<StoredRecord>> _feeds = [];

    public bool IsAvailable => true;

    public SyncState State => SyncState.Live;

    public event Action<IReadOnlySet<string>>? FeedsChanged { add { } remove { } }

    public event Action? StateChanged { add { } remove { } }

    public void Follow(IEnumerable<string> feeds)
    {
    }

    public void Put<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string feed, string id, T record, TypeModel model)
    {
        using MemoryStream stream = new();
        model.Serialize(stream, record);
        if (!_feeds.TryGetValue(feed, out List<StoredRecord>? rows))
            _feeds[feed] = rows = [];
        rows.RemoveAll(row => row.Id == id);
        rows.Add(new StoredRecord(id, 1, stream.ToArray(), null, null, null, null));
    }

    public ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query) =>
        ValueTask.FromResult<IReadOnlyList<StoredRecord>>([.. _feeds.GetValueOrDefault(query.Feed, []).Skip(query.Offset).Take(query.Limit)]);

    public ValueTask<StoreInfo> InfoAsync() => ValueTask.FromResult(new StoreInfo(0, []));

    public ValueTask RefreshAsync() => ValueTask.CompletedTask;
}

internal sealed class QueuedWrites : IOutbox
{
    public List<(string Method, byte[] Body)> Sent { get; } = [];

    public bool IsAvailable => true;

    public event Action? Changed { add { } remove { } }

    public ValueTask<string> EnqueueAsync(string method, byte[] request)
    {
        Sent.Add((method, request));
        return ValueTask.FromResult(Guid.NewGuid().ToString());
    }

    public ValueTask<string> EnqueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model)
    {
        using MemoryStream stream = new();
        model.Serialize(stream, request);
        return EnqueueAsync(method, stream.ToArray());
    }

    public ValueTask EnqueueQuietlyAsync(string method, byte[] request) => ValueTask.CompletedTask;

    public ValueTask DiscardQuietAsync() => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<OutboxEntry>> ListAsync() => ValueTask.FromResult<IReadOnlyList<OutboxEntry>>([]);

    public ValueTask RetryAsync(string id) => ValueTask.CompletedTask;

    public ValueTask DiscardAsync(string id) => ValueTask.CompletedTask;

    public List<WalkPiece> Pieces() =>
        [.. Sent.Where(write => write.Method.EndsWith("/RecordWalk", StringComparison.Ordinal))
            .Select(write => OutreachContractsModel.Instance.Deserialize<WalkPiece>(new MemoryStream(write.Body)))];
}

internal sealed class PhonePreferences : ILocalPreferences
{
    private readonly Dictionary<string, string> _values = [];

    public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(_values.GetValueOrDefault(key));

    public ValueTask SetAsync(string key, string? value)
    {
        if (value is null)
            _values.Remove(key);
        else
            _values[key] = value;
        return ValueTask.CompletedTask;
    }
}

internal sealed class LocationJournal : ILocationSession
{
    private readonly List<LocationFix> _fixes = [];

    public bool Running { get; private set; }

    public bool IsAvailable => true;

    public event Action? FixArrived { add { } remove { } }

    public void Walk(IEnumerable<Routing.GpsFix> fixes)
    {
        foreach (Routing.GpsFix fix in fixes)
            _fixes.Add(new LocationFix(_fixes.Count + 1, fix.At, fix.Point.Lat, fix.Point.Lon, fix.Accuracy, -1, -1));
    }

    public Task<LocationStart> StartAsync(string purpose)
    {
        Running = true;
        return Task.FromResult(LocationStart.Running);
    }

    public Task StopAsync()
    {
        Running = false;
        return Task.CompletedTask;
    }

    public Task<LocationStatus> StateAsync() => Task.FromResult(new LocationStatus(Running, LocationAuthorization.WhenInUse));

    public Task<LocationFixes> FixesAsync(long since) =>
        Task.FromResult(new LocationFixes([.. _fixes.Where(fix => fix.Seq > since)], _fixes.Count, Running));

    public Task ClearAsync(long upTo)
    {
        _fixes.RemoveAll(fix => fix.Seq <= upTo);
        return Task.CompletedTask;
    }
}

internal sealed class RegionFiles(byte[] streets) : IKeptFiles
{
    public bool IsAvailable => true;

    public event Action? Changed { add { } remove { } }

    private readonly Dictionary<string, KeptFile> _kept = [];

    public Task KeepAsync(string id, string url, string title)
    {
        _kept[id] = new KeptFile(id, url, title, 1, 1, KeptFileState.Done);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KeptFile>> ListAsync() => Task.FromResult<IReadOnlyList<KeptFile>>([.. _kept.Values]);

    public Task RemoveAsync(string id)
    {
        _kept.Remove(id);
        return Task.CompletedTask;
    }

    public Task<byte[]> ReadAsync(string id, long offset = 0, long length = -1) => Task.FromResult(streets);
}

internal sealed class SignedOut : IAccess
{
    public event Action? Changed { add { } remove { } }

    public bool SignedIn => false;

    public bool Can(Permission permission, string? objectId = null) => false;

    public bool CanSomewhere(Permission permission) => false;
}

internal sealed class Phone
{
    public const string RegionId = "albany-creek";

    public Phone(LocationJournal? location = null, PhonePreferences? preferences = null)
    {
        Location = location ?? new LocationJournal();
        Preferences = preferences ?? new PhonePreferences();
        Feeds.Put(OutreachFeeds.Settings, "settings", OutreachDefaults.Settings(), OutreachContractsModel.Instance);
        Feeds.Put(OutreachFeeds.Campaigns, OutreachCampaigns.GeneralId.ToString(),
            new Campaign { Id = OutreachCampaigns.GeneralId.ToString(), Name = OutreachCampaigns.GeneralName, General = true }, OutreachContractsModel.Instance);
        Feeds.Put(OutreachMapsFeeds.Regions, RegionId, new OutreachRegion
        {
            Id = RegionId, Name = "Albany Creek", Version = 1,
            Core = new MapBounds { South = -27.40, West = 152.90, North = -27.30, East = 153.02 },
            Bounds = new MapBounds { South = -27.42, West = 152.88, North = -27.28, East = 153.04 },
            Streets = new MapFile { Url = "https://media.example/streets", Bytes = 1 }, Map = new MapFile { Url = "https://media.example/map", Bytes = 1 },
        }, OutreachMapsContractsModel.Instance);
        Store = new OutreachStore(Feeds, new SignedOut());
        Regions = new RegionMaps(new RegionFiles(Fixture.AlbanyCreekBytes()), Preferences);
        Walk = new WalkSession(Location, Outbox, Preferences, Regions, Store, Clock);
    }

    public StoredFeeds Feeds { get; } = new();

    public QueuedWrites Outbox { get; } = new();

    public PhonePreferences Preferences { get; }

    public LocationJournal Location { get; }

    public OutreachStore Store { get; }

    public RegionMaps Regions { get; }

    public WalkSession Walk { get; }

    public TimeProvider Clock { get; } = new StoppedClock(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(10)));
}

internal sealed class StoppedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
