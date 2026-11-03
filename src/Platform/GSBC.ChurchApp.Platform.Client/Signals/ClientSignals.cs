using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Signals;

public sealed class ClientSignals : IDisposable
{
    private static readonly IReadOnlySet<string> NoFeeds = new HashSet<string>();

    private readonly IFeedSignals? _feeds;
    private readonly IOutbox? _outbox;
    private readonly IAccess _access;
    private readonly SessionSignal _session;

    public ClientSignals(IServiceProvider services, IAccess access, SessionSignal session)
    {
        _feeds = services.GetService<IFeedSignals>();
        _outbox = services.GetService<IOutbox>();
        _access = access;
        _session = session;
        Online = _feeds?.State != SyncState.Offline;

        if (_feeds is not null)
        {
            _feeds.FeedsChanged += OnFeedsChanged;
            _feeds.StateChanged += OnStateChanged;
        }
        if (_outbox is not null)
            _outbox.Changed += OnOutboxChanged;
        _access.Changed += OnAccessChanged;
        _session.Changed += OnSessionChanged;
    }

    public bool Online { get; private set; }

    public event Action<ClientChange>? Changed;

    public void Follow(IReadOnlyCollection<string>? feeds)
    {
        if (feeds is { Count: > 0 })
            _feeds?.Follow(feeds);
    }

    public void Dispose()
    {
        if (_feeds is not null)
        {
            _feeds.FeedsChanged -= OnFeedsChanged;
            _feeds.StateChanged -= OnStateChanged;
        }
        if (_outbox is not null)
            _outbox.Changed -= OnOutboxChanged;
        _access.Changed -= OnAccessChanged;
        _session.Changed -= OnSessionChanged;
    }

    private void OnFeedsChanged(IReadOnlySet<string> feeds) => Raise(ChangeKind.Feeds, feeds);

    private void OnStateChanged()
    {
        if (_feeds!.State == SyncState.Live && !Online)
        {
            Online = true;
            Raise(ChangeKind.Reconnected);
        }
        else if (_feeds.State == SyncState.Offline && Online)
        {
            Online = false;
            Raise(ChangeKind.Disconnected);
        }
    }

    private void OnOutboxChanged() => Raise(ChangeKind.Outbox);

    private void OnAccessChanged() => Raise(ChangeKind.Access);

    private Task OnSessionChanged()
    {
        Raise(ChangeKind.Session);
        return Task.CompletedTask;
    }

    private void Raise(ChangeKind kind, IReadOnlySet<string>? feeds = null) => Changed?.Invoke(new ClientChange(kind, feeds ?? NoFeeds));
}
