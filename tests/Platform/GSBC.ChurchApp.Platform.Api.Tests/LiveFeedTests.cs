using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class LiveFeedTests : IAsyncLifetime
{
    private const string Library = "media.library";
    private static readonly Permission SeeLibrary = new("media.view", ScopeKind.Global, "See the media library");
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private TestDatabase _database = null!;

    private readonly TestClock _clock = new();
    private readonly FeedChangeBroadcaster _broadcaster = new();
    private readonly Sessions _sessions = new();
    private readonly HashSet<string> _allowed = [];
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = await DatabaseAsync(_database.ConnectionString);
    }

    private async Task<ServiceProvider> DatabaseAsync(string connection)
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", connection)])
                .Build())
            .AddSyncData()
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton(new FeedRegistry([new FeedDefinition(Library, FeedAudience.Holding(SeeLibrary))]))
            .AddSingleton(_broadcaster)
            .AddSingleton<IAccessEvaluator>(new Allowed(_allowed))
            .AddSingleton<ISignedInSessions>(_sessions)
            .AddScoped<SyncService>()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        return services;
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task AnOpenPageHearsOfAChangeItMaySee()
    {
        await ChangeAsync("older upload");
        using Watching editor = Watch(Person(seesLibrary: true));
        await editor.ConnectedAsync();

        await ChangeAsync("new upload");

        Assert.Contains(Library, await editor.NextChangeAsync());
    }

    [Fact]
    public async Task SomeoneWithoutTheAudienceHearsNothingOfIt()
    {
        using Watching visitor = Watch(Person(seesLibrary: false));
        await visitor.ConnectedAsync();
        visitor.Forget();

        await ChangeAsync("new upload");
        using Watching editor = Watch(Person(seesLibrary: true));
        await editor.ConnectedAsync();
        await ChangeAsync("another upload");
        await editor.NextChangeAsync();

        Assert.False(visitor.HeardAny);
    }

    [Fact]
    public async Task AChangeMadeWhileTheConnectionWasDownArrivesWhenItIsBack()
    {
        using Watching editor = Watch(Person(seesLibrary: true));
        await editor.ConnectedAsync();

        editor.DropConnection();
        await ChangeAsync("uploaded during the outage");
        editor.Restore();

        Assert.Contains(Library, await editor.NextChangeAsync());
    }

    [Fact]
    public async Task AnOpenPageHearsOfItsFeedsWhenTheServerMovesToAnotherDatabase()
    {
        await ChangeAsync("older upload");
        using Watching editor = Watch(Person(seesLibrary: true));
        await editor.ConnectedAsync();
        editor.Forget();
        await using TestDatabase rebuiltDatabase = await TestDatabase.CreateAsync();
        await using ServiceProvider rebuilt = await DatabaseAsync(rebuiltDatabase.ConnectionString);
        await ChangeAsync("upload in the rebuilt database", rebuilt);

        editor.MoveTo(rebuilt);

        Assert.Contains(Library, await editor.NextChangeAsync());
    }

    [Fact]
    public async Task AStreamEndsOnceItsSignInHasEnded()
    {
        ClaimsPrincipal person = Person(seesLibrary: true);
        using CancellationTokenSource cancel = new(Patience);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IAsyncEnumerator<WatchEvent> stream = scope.ServiceProvider.GetRequiredService<SyncService>()
            .WatchAsync(new WatchRequest { Cursors = [new FeedCursor { Feed = Library, Latest = true }] }, Server(person, cancel.Token))
            .GetAsyncEnumerator(cancel.Token);
        Assert.True(await stream.MoveNextAsync());

        _sessions.End(Caller.SessionId(person)!);
        await ChangeAsync("new upload");

        RpcException ended = await Assert.ThrowsAsync<RpcException>(async () => await stream.MoveNextAsync());
        Assert.Equal(StatusCode.Unauthenticated, ended.StatusCode);
    }

    private ClaimsPrincipal Person(bool seesLibrary)
    {
        string id = Guid.CreateVersion7().ToString();
        if (seesLibrary)
            _allowed.Add(id);
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(Caller.UserIdClaim, id), new Claim(Caller.SessionIdClaim, Guid.CreateVersion7().ToString())], "test"));
    }

    private Watching Watch(ClaimsPrincipal person)
    {
        Watching watching = new(new InProcessSync(_services, person));
        watching.Watch.Follow([Library]);
        return watching;
    }

    private async Task ChangeAsync(string recordId, IServiceProvider? database = null)
    {
        await using AsyncServiceScope scope = (database ?? _services).CreateAsyncScope();
        SyncDbContext db = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        db.FeedChanges.Add(FeedChangeRecord.Marker(Library, recordId, _clock.UtcNow));
        await db.SaveChangesAsync();
        _broadcaster.PublishChange(Library);
    }

    private static CallContext Server(ClaimsPrincipal person, CancellationToken cancellationToken) =>
        new(person, new PersonCallContext(person, cancellationToken));

    internal sealed class Sessions : ISignedInSessions
    {
        private readonly ConcurrentDictionary<string, bool> _ended = [];

        public void End(string sessionId) => _ended[sessionId] = true;

        public ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(!_ended.ContainsKey(sessionId));
    }

    private sealed class Watching : IDisposable
    {
        private readonly InProcessSync _sync;
        private readonly TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly System.Threading.Channels.Channel<IReadOnlySet<string>> _changes = System.Threading.Channels.Channel.CreateUnbounded<IReadOnlySet<string>>();

        public Watching(InProcessSync sync)
        {
            _sync = sync;
            Watch = new FeedWatch(sync, new SessionSignal());
            Watch.StateChanged += () =>
            {
                if (Watch.State == SyncState.Live)
                    _connected.TrySetResult();
            };
            Watch.FeedsChanged += feeds => _changes.Writer.TryWrite(feeds);
        }

        public FeedWatch Watch { get; }

        public bool HeardAny => _changes.Reader.TryPeek(out _);

        public Task ConnectedAsync() => _connected.Task.WaitAsync(Patience);

        public async Task<IReadOnlySet<string>> NextChangeAsync()
        {
            using CancellationTokenSource timeout = new(Patience);
            return await _changes.Reader.ReadAsync(timeout.Token);
        }

        public void Forget()
        {
            while (_changes.Reader.TryRead(out _))
            {
            }
        }

        public void DropConnection() => _sync.Down = true;

        public void Restore() => _sync.Down = false;

        public void MoveTo(IServiceProvider database)
        {
            _sync.Services = database;
            DropConnection();
            Restore();
        }

        public void Dispose() => Watch.Dispose();
    }

    private sealed class InProcessSync(IServiceProvider services, ClaimsPrincipal person) : ISyncService
    {
        public IServiceProvider Services { get; set; } = services;

        private CancellationTokenSource _connection = new();

        public bool Down
        {
            get;
            set
            {
                field = value;
                if (value)
                    Interlocked.Exchange(ref _connection, new CancellationTokenSource()).Cancel();
            }
        }

        public ValueTask<PullReply> PullAsync(PullRequest request, CallContext context = default) => throw new NotSupportedException();

        public async IAsyncEnumerable<WatchEvent> WatchAsync(WatchRequest request, CallContext context = default)
        {
            if (Down)
                throw new RpcException(new Status(StatusCode.Unavailable, "No connection."));

            using CancellationTokenSource call = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, _connection.Token);
            await using AsyncServiceScope scope = Services.CreateAsyncScope();
            await foreach (WatchEvent message in Stream(scope.ServiceProvider.GetRequiredService<SyncService>(), request, call.Token))
                yield return message;
        }

        private async IAsyncEnumerable<WatchEvent> Stream(SyncService sync, WatchRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (WatchEvent message in sync.WatchAsync(request, Server(person, cancellationToken)).WithCancellation(cancellationToken))
                yield return message;
        }
    }

    private sealed class Allowed(HashSet<string> people) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(node == SeeLibrary.Node && Caller.UserId(user) is { } id && people.Contains(id.ToString()));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }

    private sealed class PersonCallContext(ClaimsPrincipal person, CancellationToken cancellationToken) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
