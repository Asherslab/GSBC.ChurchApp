using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Push;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class NotificationsTests : IAsyncLifetime
{
    private const string Music = "music";
    private static readonly Permission ReceiveNotices = new("ministries.notices.receive", ScopeKind.Ministry, "Get notices");
    private static readonly NotificationKind Notice = new("ministries.notice", "Important notices", "Ministries",
        PerScope: ScopeKind.Ministry, MayBeUrgent: true, Pacing: new NotificationPacing(3, TimeSpan.FromHours(1)));
    private static readonly NotificationKind NewPasskey = new("accounts.passkey.added", "A passkey was added to your account", "Your account",
        Mandatory: true);
    private static readonly NotificationKind ChatMessage = new("ministries.chat", "New messages in your chats", "Ministries", PerScope: ScopeKind.Ministry);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly Clock _clock = new(ChurchTime.ToUtc(new DateTime(2026, 11, 8, 10, 0, 0)));
    private readonly Holders _holders = new();
    private readonly RecordingTransport _transport = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton(new NotificationKindCatalog([new NotificationKindSet([Notice, NewPasskey, ChatMessage, .. AnnouncementNotifications.All])]))
            .AddSingleton<PushSignal>()
            .AddSingleton<IPermissionHolders>(_holders)
            .AddSingleton<IPeopleIndex>(_holders)
            .AddSingleton<IPushTransport>(_transport)
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
        await _postgres.DisposeAsync();
    }

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ANotificationAboutAThingMustOpenThatThing()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => RaiseAsync(new Notification(Notice, NotificationAudience.Everyone, "Room change", "",
            Subject: "ministry/music/notice/1")));
        await Assert.ThrowsAsync<ArgumentException>(() => RaiseAsync(new Notification(Notice, NotificationAudience.Everyone, "Room change", "",
            Link: "https://example.com", Subject: "ministry/music/notice/1")));

        await RaiseAsync(new Notification(Notice, NotificationAudience.Everyone, "Room change", "", Link: "/ministries/music/news", Subject: "ministry/music/notice/1"));
    }

    [Fact]
    public async Task FollowersAndMembersHearFromTheirMinistryAndAPhoneSeesItWithoutSigningIn()
    {
        Guid member = Guid.CreateVersion7(), outsider = Guid.CreateVersion7(), followingPhone = Guid.CreateVersion7(), otherPhone = Guid.CreateVersion7();
        _holders.Grant(ReceiveNotices, Music, people: [member], phones: [followingPhone]);
        _holders.Everyone = [member, outsider];
        await RegisterAsync(followingPhone, null);
        await RegisterAsync(otherPhone, null);
        Guid membersPhone = Guid.CreateVersion7();
        await RegisterAsync(membersPhone, member);

        await RaiseAsync(new Notification(Notice, NotificationAudience.Holding(ReceiveNotices, Music), "No practice tonight", "",
            Link: "/ministries/music/news", Subject: "ministry/music/notice/1", ScopeId: Music));
        await DeliverAsync();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        Assert.Equal([member], await db.Inbox.Select(entry => entry.UserId).ToListAsync(Cancel));
        FeedChangeRecord publicRow = Assert.Single(await db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Public).ToListAsync(Cancel));
        Assert.Equal(Music, publicRow.GroupKey);
        Assert.Equal(new HashSet<string> { Token(followingPhone), Token(membersPhone) }, _transport.Sent.Select(push => push.Target.Token).ToHashSet());
        Assert.All(_transport.Sent, push => Assert.Equal("/ministries/music/news", push.Message.Link));
    }

    [Fact]
    public async Task TurningAKindOffStopsItsPushButNotItsInboxAndSafetyNoticesAlwaysPush()
    {
        Guid person = Guid.CreateVersion7(), phone = Guid.CreateVersion7();
        await RegisterAsync(phone, person);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            db.Preferences.AddRange(
                new Preference { Id = Guid.CreateVersion7(), UserId = person, Kind = ChatMessage.Key, ScopeId = Music, Push = false },
                new Preference { Id = Guid.CreateVersion7(), UserId = person, Kind = NewPasskey.Key, Push = false });
            await db.SaveChangesAsync(Cancel);
        }

        await RaiseAsync(new Notification(ChatMessage, NotificationAudience.Person(person), "Sam: see you at 6", "", Link: "/ministries/music/chats/1",
            Subject: "chat/1", ScopeId: Music));
        await RaiseAsync(new Notification(NewPasskey, NotificationAudience.Person(person), "A passkey was added", "On an iPhone", Link: "/account",
            Subject: "account/passkeys"));
        await DeliverAsync();

        Assert.Equal(["A passkey was added"], _transport.Sent.Select(push => push.Message.Title));
        Assert.Equal(2, await CountAsync(db => db.Inbox.Where(entry => entry.UserId == person)));
    }

    [Fact]
    public async Task OvernightPushesWaitForMorningUnlessUrgent()
    {
        Guid person = Guid.CreateVersion7();
        await RegisterAsync(Guid.CreateVersion7(), person);
        _clock.Now = ChurchTime.ToUtc(new DateTime(2026, 11, 8, 22, 30, 0));

        await RaiseAsync(new Notification(Notice, NotificationAudience.Person(person), "Bring a plate on Sunday", ""));
        await RaiseAsync(new Notification(Notice, NotificationAudience.Person(person), "Tonight's service is cancelled", "", Urgent: true));
        await DeliverAsync();
        Assert.Equal(["Tonight's service is cancelled"], _transport.Sent.Select(push => push.Message.Title));

        _clock.Now = ChurchTime.ToUtc(new DateTime(2026, 11, 9, 7, 0, 0));
        await DeliverAsync();
        Assert.Equal("Bring a plate on Sunday", _transport.Sent[^1].Message.Title);
    }

    [Fact]
    public async Task PastAKindsPaceTheRestArriveAsOneSummary()
    {
        Guid person = Guid.CreateVersion7();
        await RegisterAsync(Guid.CreateVersion7(), person);
        for (int notice = 1; notice <= 5; notice++)
            await RaiseAsync(new Notification(Notice, NotificationAudience.Person(person), $"Notice {notice}", "", ScopeId: Music));
        await DeliverAsync();
        Assert.Equal(3, _transport.Sent.Count);

        _clock.Now += TimeSpan.FromMinutes(11);
        await DeliverAsync();

        PushMessage summary = Assert.Single(_transport.Sent.Skip(3)).Message;
        Assert.Equal("2 new · Notice 5", summary.Body);
        Assert.Equal(5, summary.Badge);
    }

    [Fact]
    public async Task ADeadTokenIsDroppedAndAFailedSendIsRetried()
    {
        Guid gone = Guid.CreateVersion7(), flaky = Guid.CreateVersion7(), person = Guid.CreateVersion7();
        _holders.Everyone = [person];
        await RegisterAsync(gone, person);
        await RegisterAsync(flaky, null);
        _transport.Outcomes[Token(gone)] = new Queue<PushResult>([new PushResult(PushOutcome.DeadToken, "Unregistered")]);
        _transport.Outcomes[Token(flaky)] = new Queue<PushResult>([new PushResult(PushOutcome.Retry, "Service unavailable")]);

        await RaiseAsync(new Notification(AnnouncementNotifications.Announcement, NotificationAudience.Everyone, "Carols at 7", ""));
        await DeliverAsync();
        Assert.Equal(1, await CountAsync(db => db.Devices));

        _clock.Now += TimeSpan.FromMinutes(1);
        await DeliverAsync();
        Assert.Equal(Token(flaky), _transport.Sent[^1].Target.Token);
        Assert.Equal(PushOutcome.Sent, _transport.Sent[^1].Result.Outcome);
    }

    [Fact]
    public async Task ReadingAnItemReachesThePersonsOtherDevices()
    {
        Guid person = Guid.CreateVersion7();
        await RaiseAsync(new Notification(Notice, NotificationAudience.Person(person), "Choir at 6", ""));
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        InboxEntry entry = await db.Inbox.SingleAsync(Cancel);

        await scope.ServiceProvider.GetRequiredService<NotificationsService>()
            .MarkReadAsync(new MarkReadRequest { Ids = [entry.Id.ToString()] }, CallAs(person));

        FeedChangeRecord latest = await db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Inbox).OrderByDescending(change => change.Cursor).FirstAsync(Cancel);
        InboxItem item = NotificationsContractsModel.Instance.Deserialize<InboxItem>(new MemoryStream(latest.Payload));
        Assert.Equal((2L, person.ToString()), (latest.Version, latest.ScopeId));
        Assert.NotNull(item.ReadAt);
    }

    private async Task RaiseAsync(Notification notification)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotificationRaiser>().RaiseAsync(notification, Cancel);
    }

    private async Task DeliverAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        PushDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<PushDispatcher>();
        while (await dispatcher.DeliverDueAsync(_clock.GetUtcNow().UtcDateTime, Cancel) > 0)
        {
        }
    }

    private async Task RegisterAsync(Guid installId, Guid? userId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        NotificationsDbContext db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        db.Devices.Add(new Device
        {
            Id = Guid.CreateVersion7(), InstallId = installId, UserId = userId, Platform = "ios", AppId = "au.com.baptist.app.dev",
            Environment = ApnsTransport.SandboxEnvironment, Token = Token(installId), RegisteredAt = _clock.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(Cancel);
    }

    private async Task<int> CountAsync<T>(Func<NotificationsDbContext, IQueryable<T>> query)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<NotificationsDbContext>()).CountAsync(Cancel);
    }

    private static string Token(Guid installId) => installId.ToString("N");

    private static ProtoBuf.Grpc.CallContext CallAs(Guid person) => new(person, new SignedInCallContext(person));

    private sealed class Clock(DateTime start) : TimeProvider
    {
        public DateTime Now { get; set; } = start;

        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private sealed class Holders : IPermissionHolders, IPeopleIndex
    {
        private readonly Dictionary<(string, string?), PermissionHolders> _granted = [];

        public IReadOnlyList<Guid> Everyone { get; set; } = [];

        public void Grant(Permission permission, string? objectId, IReadOnlyList<Guid> people, IReadOnlyList<Guid> phones) =>
            _granted[(permission.Node, objectId)] = new PermissionHolders(people, phones);

        public ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(_granted.GetValueOrDefault((permission.Node, objectId), PermissionHolders.None));

        public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Everyone);
    }

    private sealed class RecordingTransport : IPushTransport
    {
        public List<(PushTarget Target, PushMessage Message, PushResult Result)> Sent { get; } = [];

        public Dictionary<string, Queue<PushResult>> Outcomes { get; } = [];

        public Task<PushResult> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken)
        {
            PushResult result = Outcomes.TryGetValue(target.Token, out Queue<PushResult>? queued) && queued.Count > 0 ? queued.Dequeue() : PushResult.Sent;
            Sent.Add((target, message, result));
            return Task.FromResult(result);
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
