using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Client.Feeds;

public sealed class FeedWatch : IFeedSignals, IDisposable
{
    private static readonly TimeSpan Silence = TimeSpan.FromSeconds(75);
    private static readonly TimeSpan MissedHeartbeat = TimeSpan.FromSeconds(40);
    private static readonly TimeSpan MaximumBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan GatherFollows = TimeSpan.FromMilliseconds(50);

    private readonly ISyncService _sync;
    private readonly SessionSignal _session;
    private readonly Lock _gate = new();
    private readonly HashSet<string> _followed = [];
    private readonly Dictionary<string, long> _cursors = [];
    private readonly HashSet<string> _hidden = [];
    private readonly CancellationTokenSource _stopped = new();
    private CancellationTokenSource _interrupt = new();
    private string? _origin;
    private bool _started;
    private DateTime _lastHeard;

    public FeedWatch(ISyncService sync, SessionSignal session)
    {
        _sync = sync;
        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public SyncState State { get; private set; } = SyncState.Connecting;

    public event Action<IReadOnlySet<string>>? FeedsChanged;

    public event Action? StateChanged;

    public void Follow(IEnumerable<string> feeds)
    {
        bool added = false;
        bool start;
        lock (_gate)
        {
            foreach (string feed in feeds)
                added |= _followed.Add(feed);
            start = added && !_started;
            _started |= added;
        }

        if (start)
            _ = RunAsync();
        else if (added)
            Interrupt();
    }

    public void Wake()
    {
        if (State != SyncState.Live || DateTime.UtcNow - _lastHeard > MissedHeartbeat)
            Interrupt();
    }

    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
        _stopped.Cancel();
    }

    private async Task RunAsync()
    {
        await PauseAsync(GatherFollows, _stopped.Token);
        int failures = 0;
        while (!_stopped.IsCancellationRequested)
        {
            CancellationToken interrupted = CurrentInterrupt();
            (WatchRequest request, HashSet<string> fromLatest) = NextRequest();
            bool heard = false;
            using (CancellationTokenSource call = CancellationTokenSource.CreateLinkedTokenSource(interrupted, _stopped.Token))
            {
                try
                {
                    call.CancelAfter(Silence);
                    await foreach (WatchEvent message in _sync.WatchAsync(request, new CallContext(new CallOptions(cancellationToken: call.Token))).WithCancellation(call.Token))
                    {
                        call.CancelAfter(Silence);
                        _lastHeard = DateTime.UtcNow;
                        failures = 0;
                        HashSet<string> changed = Apply(message);
                        bool first = !heard;
                        heard = true;
                        if (first)
                            changed.UnionWith(fromLatest);
                        if (changed.Count > 0)
                            FeedsChanged?.Invoke(changed);
                        if (first)
                            SetState(SyncState.Live);
                    }
                }
                catch (Exception) when (!_stopped.IsCancellationRequested)
                {
                }
            }

            if (_stopped.IsCancellationRequested || interrupted.IsCancellationRequested)
                continue;

            failures++;
            if (failures == 1)
            {
                SetState(SyncState.Connecting);
                continue;
            }
            SetState(SyncState.Offline);
            using CancellationTokenSource wait = CancellationTokenSource.CreateLinkedTokenSource(interrupted, _stopped.Token);
            await PauseAsync(Backoff(failures), wait.Token);
        }
    }

    private (WatchRequest Request, HashSet<string> FromLatest) NextRequest()
    {
        lock (_gate)
        {
            HashSet<string> fromLatest = _followed.Where(feed => !_cursors.ContainsKey(feed)).ToHashSet();
            WatchRequest request = new()
            {
                Cursors = _followed
                    .Select(feed => _cursors.TryGetValue(feed, out long cursor) ? new FeedCursor { Feed = feed, Cursor = cursor } : new FeedCursor { Feed = feed, Latest = true })
                    .ToList(),
                Origin = _origin,
            };
            return (request, fromLatest);
        }
    }

    private HashSet<string> Apply(WatchEvent message)
    {
        HashSet<string> changed = [];
        lock (_gate)
        {
            if (!string.IsNullOrEmpty(message.Origin))
                _origin = message.Origin;
            foreach (FeedBatch batch in message.Feeds)
            {
                _cursors[batch.Feed] = batch.Cursor;
                bool hidden = batch.Reset && !batch.HasMore && batch.Changes.Count == 0;
                bool newlyHidden = hidden && _hidden.Add(batch.Feed);
                if (!hidden)
                    _hidden.Remove(batch.Feed);
                if (newlyHidden || (!hidden && (batch.Reset || batch.Changes.Count > 0)))
                    changed.Add(batch.Feed);
            }
        }
        return changed;
    }

    private CancellationToken CurrentInterrupt()
    {
        lock (_gate)
            return _interrupt.Token;
    }

    private void Interrupt()
    {
        CancellationTokenSource interrupted;
        lock (_gate)
        {
            interrupted = _interrupt;
            _interrupt = new CancellationTokenSource();
        }
        interrupted.Cancel();
    }

    private Task OnSessionChanged()
    {
        Interrupt();
        return Task.CompletedTask;
    }

    private void SetState(SyncState state)
    {
        if (State == state)
            return;
        State = state;
        StateChanged?.Invoke();
    }

    private static TimeSpan Backoff(int failures) =>
        TimeSpan.FromSeconds(Math.Min(MaximumBackoff.TotalSeconds, Math.Pow(2, Math.Min(failures - 1, 5))) * (0.5 + Random.Shared.NextDouble() / 2));

    private static async Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
