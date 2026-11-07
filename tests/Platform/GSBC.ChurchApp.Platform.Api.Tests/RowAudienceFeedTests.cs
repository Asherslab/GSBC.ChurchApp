using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class RowAudienceFeedTests : IAsyncLifetime
{
    private const string Feed = "events.private";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly Dictionary<Guid, GrantSet> _grants = [];
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSyncData()
            .AddSingleton(new FeedRegistry([new FeedDefinition(Feed, FeedAudience.EachRow)]))
            .AddSingleton(new FeedChangeBroadcaster())
            .AddSingleton<IAccessEvaluator>(new HeldRoles(_grants))
            .AddSingleton<ISignedInSessions>(new LiveFeedTests.Sessions())
            .AddScoped<SyncService>()
            .BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ARowReachesOnlyThoseItsAudienceIncludesAndASignedOutPhoneNone()
    {
        Guid churchMember = Guid.CreateVersion7();
        await AddAsync("agm", Audience.HoldingRole(churchMember));
        await AddAsync("helpers night", Audience.SignedIn);
        await AddAsync("no audience", null);

        Assert.Equal(["agm", "helpers night"], Shown(await ReadAsync(Person(churchMember))));
        Assert.Equal(["helpers night"], Shown(await ReadAsync(Person())));
        Assert.Empty((await ReadAsync(new ClaimsPrincipal())).Feeds.SelectMany(batch => batch.Changes));
    }

    [Fact]
    public async Task SomeoneWhoLostARowsAudienceIsToldItsGoneEvenAfterItsTombstoneWasPruned()
    {
        Guid kidsTeam = Guid.CreateVersion7();
        Guid leaders = Guid.CreateVersion7();
        ClaimsPrincipal helper = Person(kidsTeam);
        await AddAsync("helpers night", Audience.HoldingRole(kidsTeam));
        long synced = (await ReadAsync(helper)).Feeds.Single().Cursor;

        await AddAsync("helpers night", null, deleted: true);
        await AddAsync("helpers night", Audience.HoldingRole(leaders));
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            Assert.True(await new FeedRetention(scope.ServiceProvider.GetRequiredService<SyncDbContext>()).PruneAsync(DateTime.UtcNow, TestContext.Current.CancellationToken));

        FeedChange change = Assert.Single((await ReadAsync(helper, synced)).Feeds.Single().Changes);
        Assert.Equal(("helpers night", true), (change.Id, change.Deleted));
        Assert.Empty(change.Payload);
    }

    private ClaimsPrincipal Person(params Guid[] roles)
    {
        Guid id = Guid.CreateVersion7();
        _grants[id] = new GrantSet(roles.Select(role => new Grant(PermissionPattern.Parse(RoleMembership.NodeOf(role)), Scope.Global)).ToList());
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(Caller.UserIdClaim, id.ToString())], "test"));
    }

    private async Task AddAsync(string id, Audience? audience, bool deleted = false)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        SyncDbContext db = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        db.FeedChanges.Add(new FeedChangeRecord { Feed = Feed, RecordId = id, Version = 1, ScopeId = audience?.Text, Deleted = deleted, Payload = deleted ? [] : [1] });
        await db.SaveChangesAsync();
    }

    private async Task<PullReply> ReadAsync(ClaimsPrincipal person, long cursor = 0)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncService>()
            .PullAsync(new PullRequest { Cursors = [new FeedCursor { Feed = Feed, Cursor = cursor }] }, new CallContext(person, new Context(person)));
    }

    private static List<string> Shown(PullReply reply) =>
        reply.Feeds.SelectMany(batch => batch.Changes).Where(change => !change.Deleted).Select(change => change.Id).Order(StringComparer.Ordinal).ToList();

    private sealed class HeldRoles(Dictionary<Guid, GrantSet> grants) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) => ValueTask.FromResult(false);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(grants.GetValueOrDefault(userId, GrantSet.Empty));
    }

    private sealed class Context(ClaimsPrincipal person) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

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
