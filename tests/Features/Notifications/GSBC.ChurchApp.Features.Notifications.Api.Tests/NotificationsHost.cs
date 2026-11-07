using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public abstract class NotificationsHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    private protected TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 8, 10, 0, 0)));
    private protected Holders Holders { get; } = new();
    private protected RecordingTransport Transport { get; } = new();

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton(TestKinds.Catalog)
            .AddSingleton<PushSignal>()
            .AddSingleton<IPermissionHolders>(Holders)
            .AddSingleton<IPeopleIndex>(Holders)
            .AddSingleton<IPushTransport>(Transport)
            .AddScoped<NotificationRaiser>()
            .AddScoped<PushDispatcher>()
            .AddScoped<NotificationsService>()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>))
            .AddSyncData()
            .AddNotificationsData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    private protected async Task<T> WithAsync<TService, T>(Func<TService, Task<T>> use) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private protected Task RaiseAsync(Notification notification) =>
        WithAsync<NotificationRaiser, Guid>(raiser => raiser.RaisedAsync(notification, Cancel));

    private protected Task<bool> DeliverAsync() =>
        WithAsync<PushDispatcher, bool>(async dispatcher =>
        {
            while (await dispatcher.DeliverDueAsync(Clock.GetUtcNow().UtcDateTime, Cancel) > 0)
            {
            }
            return true;
        });

    private protected Task<int> RegisterAsync(Guid installId, Guid? userId) =>
        WithAsync<NotificationsDbContext, int>(db =>
        {
            db.Devices.Add(new Device
            {
                Id = Guid.CreateVersion7(), InstallId = installId, UserId = userId, Platform = "ios", AppId = "au.com.baptist.app.dev",
                Environment = ApnsTransport.SandboxEnvironment, Token = Token(installId), RegisteredAt = Clock.GetUtcNow().UtcDateTime,
            });
            return db.SaveChangesAsync(Cancel);
        });

    private protected Task<List<T>> ListAsync<T>(Func<NotificationsDbContext, IQueryable<T>> query) =>
        WithAsync<NotificationsDbContext, List<T>>(db => query(db).ToListAsync(Cancel));

    private protected async Task<FeedChangeRecord> LatestInboxRowAsync() =>
        (await ListAsync(db => db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Inbox).OrderByDescending(change => change.Cursor).Take(1))).Single();

    private protected static InboxItem Item(FeedChangeRecord row) => NotificationsContractsModel.Instance.Deserialize<InboxItem>(new MemoryStream(row.Payload));

    private protected static string Token(Guid installId) => installId.ToString("N");

    private protected static ProtoBuf.Grpc.CallContext CallAs(Guid person) => new(person, new SignedInCallContext(person));

    private protected sealed class RecordingTransport : IPushTransport
    {
        private readonly Lock _sending = new();
        private readonly List<(PushTarget Target, PushMessage Message, PushResult Result)> _sent = [];

        public IReadOnlyList<(PushTarget Target, PushMessage Message, PushResult Result)> Sent
        {
            get
            {
                lock (_sending)
                    return [.. _sent];
            }
        }

        public Dictionary<string, Queue<PushResult>> Outcomes { get; } = [];

        public Task<PushResult> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken)
        {
            lock (_sending)
            {
                PushResult result = Outcomes.TryGetValue(target.Token, out Queue<PushResult>? queued) && queued.Count > 0 ? queued.Dequeue() : PushResult.Sent;
                _sent.Add((target, message, result));
                return Task.FromResult(result);
            }
        }
    }

    private sealed class SignedInCallContext(Guid person) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new()
        {
            ["__HttpContext"] = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Caller.UserIdClaim, person.ToString())], "test")) },
        };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => TestContext.Current.CancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
