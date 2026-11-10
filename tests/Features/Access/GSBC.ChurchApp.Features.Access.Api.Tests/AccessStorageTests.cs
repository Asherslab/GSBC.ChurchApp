using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using static GSBC.ChurchApp.Features.Access.Api.Tests.TestChurch;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

[Integration("Assignments live in Postgres: expired ones are left out, moves rewrite rows without duplicates, and each person's grants go out as their own feed rows")]
public sealed class AccessStorageTests : IAsyncLifetime
{
    private TestDatabase _database = null!;

    private readonly TestClock _clock = new();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        PermissionCatalog catalog = TestChurch.NewCatalog();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton(catalog)
            .AddSingleton<GrantCache>()
            .AddHttpContextAccessor()
            .AddSingleton<ToolCallers>()
            .AddSingleton<IScopeHierarchy>(new MusicOverChoir())
            .AddScoped<AccessEvaluator>()
            .AddScoped<IAccessEvaluator>(provider => provider.GetRequiredService<AccessEvaluator>())
            .AddScoped<AssignmentRules>()
            .AddScoped<AccessInsights>()
            .AddScoped<RoleAssignments>()
            .AddScoped<IRoleAssignments>(provider => provider.GetRequiredService<RoleAssignments>())
            .AddScoped<IPhoneRoles>(provider => provider.GetRequiredService<RoleAssignments>())
            .AddScoped<IPermissionHolders, PermissionHolderLookup>()
            .AddScoped<GrantsFeed>()
            .AddSingleton<HeldThroughRules>()
            .AddScoped<AccessChanges>()
            .AddSingleton(new FeedRegistry([new FeedDefinition(AccessFeeds.Grants, FeedAudience.OwnRecords)]))
            .AddSingleton<FeedChangeBroadcaster>()
            .AddScoped<SyncService>()
            .AddSingleton<ISignedInSessions, AlwaysSignedIn>()
            .AddSyncData()
            .AddAccessData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        await db.Database.MigrateAsync();
        await ReconcileAsync(TestChurch.SystemRoles);
    }

    [Fact]
    public async Task ARenamedRoleKeepsItsIdSoItsHoldersAndAnythingAimedAtItCarryAcross()
    {
        Guid person = Guid.CreateVersion7();
        Guid accessManager = await SystemRoleAsync("access-manager");
        await AssignAsync(person, accessManager);

        await ReconcileAsync([.. SystemRoles.Where(role => role.Key != "access-manager"),
            new SystemRole("roles-manager", "Roles Manager", "", ScopeKind.Global, ["access.view"], Replaces: ["access-manager"])]);

        Assert.Equal(accessManager, await SystemRoleAsync("roles-manager"));
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        Assert.Equal([new RoleHolder(person)],
            await scope.ServiceProvider.GetRequiredService<IRoleAssignments>().HoldersAsync("roles-manager", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ARoleSplitOffStartsWithTheOldRolesHoldersOnlyWhenItIsFirstMade()
    {
        Guid person = Guid.CreateVersion7(), later = Guid.CreateVersion7();
        await AssignAsync(person, await SystemRoleAsync("account-manager"));
        SystemRole[] split = [.. SystemRoles, new("app-review", "App Review Manager", "", ScopeKind.Global, ["accounts.view"], StartsWithHoldersOf: ["account-manager"])];

        await ReconcileAsync(split);
        await AssignAsync(later, await SystemRoleAsync("account-manager"));
        await ReconcileAsync(split);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        Assert.Equal([new RoleHolder(person)],
            await scope.ServiceProvider.GetRequiredService<IRoleAssignments>().HoldersAsync("app-review", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RolesMadeHereGoUnlessSomethingIsStillAimedAtThem()
    {
        Guid aimedAt = Guid.CreateVersion7(), unused = Guid.CreateVersion7();
        await using (AsyncServiceScope setup = _services.CreateAsyncScope())
        {
            AccessDbContext db = setup.ServiceProvider.GetRequiredService<AccessDbContext>();
            db.Roles.AddRange(
                new RoleRecord { Id = aimedAt, Name = "Youth team", ScopeKind = ScopeKind.Global.Name, CreatedAt = _clock.UtcNow },
                new RoleRecord { Id = unused, Name = "Test role", ScopeKind = ScopeKind.Global.Name, CreatedAt = _clock.UtcNow });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using ServiceProvider withUses = new ServiceCollection().AddSingleton(_services.GetRequiredService<IConfiguration>()).AddAccessData()
            .AddSingleton<IRoleUses>(new AimedAt(aimedAt)).BuildServiceProvider();
        await using AsyncServiceScope scope = withUses.CreateAsyncScope();
        await Reconciler(SystemRoles).RemoveMadeHereRolesAsync(scope.ServiceProvider, TestContext.Current.CancellationToken);

        await using AsyncServiceScope check = _services.CreateAsyncScope();
        Assert.Equal([aimedAt], await check.ServiceProvider.GetRequiredService<AccessDbContext>().Roles
            .Where(role => role.SystemKey == null).Select(role => role.Id).ToListAsync(TestContext.Current.CancellationToken));
    }

    private AccessReconciler Reconciler(IEnumerable<SystemRole> roles) =>
        new(_services.GetRequiredService<IServiceScopeFactory>(), _services.GetRequiredService<PermissionCatalog>(), roles, TestChurch.Additions, TestChurch.Kits, _clock,
            NullLogger<AccessReconciler>.Instance);

    private async Task ReconcileAsync(IEnumerable<SystemRole> roles)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await Reconciler(roles).ReconcileAsync(scope.ServiceProvider.GetRequiredService<AccessDbContext>(), TestContext.Current.CancellationToken);
    }

    private sealed class AimedAt(Guid roleId) : IRoleUses
    {
        public ValueTask<IReadOnlyList<string>> UsesAsync(Guid role, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(role == roleId ? ["a Home screen tile"] : []);
    }

    [Fact]
    public async Task AnExpiredAssignmentNoLongerCounts()
    {
        Guid person = Guid.CreateVersion7();
        await AssignAsync(person, await SystemRoleAsync("leader"), scopeObjectId: Music);
        await AssignAsync(person, await SystemRoleAsync(AccessService.OwnerKey), expiresAt: _clock.UtcNow.AddDays(-1));

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        GrantSet grants = await scope.ServiceProvider.GetRequiredService<AccessEvaluator>().GrantsForAsync(person, TestContext.Current.CancellationToken);

        Assert.True(grants.IsAllowed(PostNews, Music));
        Assert.False(grants.IsAllowed(AccessPermissions.View));
    }

    [Fact]
    public async Task APhoneFollowsWithoutAnAccountAndTheFollowMovesToThePersonWhoSignsIn()
    {
        Guid phone = Guid.CreateVersion7(), signsIn = Guid.CreateVersion7();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IPhoneRoles phones = scope.ServiceProvider.GetRequiredService<IPhoneRoles>();
        IRoleAssignments roles = scope.ServiceProvider.GetRequiredService<IRoleAssignments>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await phones.GiveAsync(phone, "follower", Music, null, cancellationToken);

        Assert.Equal(("follower", Music), Assert.Single((await phones.HeldAsync(phone, cancellationToken)).Select(held => (held.RoleKey, held.ObjectId))));
        Assert.Empty(await roles.HoldersAsync("follower", Music, cancellationToken));

        await phones.MoveToPersonAsync(phone, signsIn, cancellationToken);

        Assert.Empty(await phones.HeldAsync(phone, cancellationToken));
        Assert.Equal([new RoleHolder(signsIn)], await roles.HoldersAsync("follower", Music, cancellationToken));
    }

    [Fact]
    public async Task WhenAScopedThingGetsANewIdItsPeopleAndPhonesMoveWithItAndNoOneHoldsItTwice()
    {
        Guid phone = Guid.CreateVersion7(), follower = Guid.CreateVersion7(), both = Guid.CreateVersion7();
        const string Renamed = "music-ministry";
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IPhoneRoles phones = scope.ServiceProvider.GetRequiredService<IPhoneRoles>();
        IRoleAssignments roles = scope.ServiceProvider.GetRequiredService<IRoleAssignments>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await phones.GiveAsync(phone, "follower", Music, null, cancellationToken);
        await roles.GiveAsFeatureAsync(follower, "follower", Music, null, cancellationToken);
        await roles.GiveAsFeatureAsync(both, "follower", Music, null, cancellationToken);
        await roles.GiveAsFeatureAsync(both, "follower", Renamed, null, cancellationToken);

        await roles.MoveScopeAsync(ScopeKind.Ministry, Music, Renamed, cancellationToken);

        Assert.Equal(Renamed, Assert.Single(await phones.HeldAsync(phone, cancellationToken)).ObjectId);
        Assert.Equal([follower, both], (await roles.HoldersAsync("follower", Renamed, cancellationToken)).Select(holder => holder.UserId).ToHashSet());
        Assert.Empty(await roles.HoldersAsync("follower", Music, cancellationToken));
    }

    [Fact]
    public async Task WhenAScopedThingIsRemovedNoOneHoldsARoleThereAnyMoreAndOtherPlacesKeepTheirs()
    {
        Guid phone = Guid.CreateVersion7(), follower = Guid.CreateVersion7(), elsewhere = Guid.CreateVersion7();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IPhoneRoles phones = scope.ServiceProvider.GetRequiredService<IPhoneRoles>();
        IRoleAssignments roles = scope.ServiceProvider.GetRequiredService<IRoleAssignments>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await phones.GiveAsync(phone, "follower", Music, null, cancellationToken);
        await roles.GiveAsFeatureAsync(follower, "follower", Music, null, cancellationToken);
        await roles.GiveAsFeatureAsync(elsewhere, "follower", Choir, null, cancellationToken);
        int before = await roles.HoldingCountAsync("follower", Music, cancellationToken);

        await roles.ForgetScopeAsync(ScopeKind.Ministry, [Music], cancellationToken);

        Assert.Equal(2, before);
        Assert.Equal(0, await roles.HoldingCountAsync("follower", Music, cancellationToken));
        Assert.Empty(await phones.HeldAsync(phone, cancellationToken));
        Assert.Equal([new RoleHolder(elsewhere)], await roles.HoldersAsync("follower", Choir, cancellationToken));
    }

    [Fact]
    public async Task APhoneReceivesOnlyItsOwnPersonsGrantsAndHearsWhenTheyChange()
    {
        Guid staff = Guid.CreateVersion7(), leader = Guid.CreateVersion7(), member = Guid.CreateVersion7();
        await AssignAsync(staff, await SystemRoleAsync("staff"));
        Assert.True((await GiveAsync(staff, leader, "leader", Music)).Done);
        Assert.True((await GiveAsync(staff, member, "member", Choir)).Done);

        FeedBatch leadersPhone = await PullGrantsAsync(Person(leader), 0);
        FeedChange leaders = Assert.Single(leadersPhone.Changes);
        Assert.Equal(leader.ToString(), leaders.Id);
        Assert.Contains(Decode(leaders), grant => grant is { Pattern: "ministries.*", ObjectId: Music });
        Assert.DoesNotContain(Decode(leaders), grant => grant is { ObjectId: Choir, Pattern: "ministries.news.read" });
        Assert.Equal([member.ToString()], (await PullGrantsAsync(Person(member), 0)).Changes.Select(change => change.Id));

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<IRoleAssignments>()
                .TakeAwayAsync(staff, leader, "leader", Music, TestContext.Current.CancellationToken)).Done);

        FeedChange taken = Assert.Single((await PullGrantsAsync(Person(leader), leadersPhone.Cursor)).Changes);
        Assert.Empty(Decode(taken));
    }

    [Fact]
    public async Task ASignedOutPhoneIsToldToDropPrivateFeedsAndReceivesNothing()
    {
        Guid staff = Guid.CreateVersion7();
        await AssignAsync(staff, await SystemRoleAsync("staff"));
        Assert.True((await GiveAsync(staff, Guid.CreateVersion7(), "leader", Music)).Done);
        ClaimsPrincipal signedOut = new(new ClaimsIdentity());

        FeedBatch fromScratch = await PullGrantsAsync(signedOut, 0);
        FeedBatch afterSigningOut = await PullGrantsAsync(signedOut, 42);

        Assert.Empty(fromScratch.Changes);
        Assert.True(afterSigningOut.Reset);
        Assert.Empty(afterSigningOut.Changes);
    }

    private async Task<FeedBatch> PullGrantsAsync(ClaimsPrincipal caller, long cursor)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        PullReply reply = await scope.ServiceProvider.GetRequiredService<SyncService>().PullAsync(
            new PullRequest { Cursors = [new FeedCursor { Feed = AccessFeeds.Grants, Cursor = cursor }] },
            new CallContext(caller, new PersonCallContext(caller)));
        return reply.Feeds.SingleOrDefault() ?? new FeedBatch { Feed = AccessFeeds.Grants, Cursor = cursor };
    }

    private static ClaimsPrincipal Person(Guid userId) => new(new ClaimsIdentity(
        [new Claim(Caller.UserIdClaim, userId.ToString()), new Claim(Caller.SessionIdClaim, Guid.CreateVersion7().ToString())], "test"));

    private static List<GrantMessage> Decode(FeedChange change) =>
        AccessContractsModel.Instance.Deserialize<GrantList>(new MemoryStream(change.Payload)).Grants;

    private async Task<AssignmentOutcome> GiveAsync(Guid actor, Guid user, string roleKey, string objectId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IRoleAssignments>()
            .GiveAsync(actor, user, roleKey, objectId, null, TestContext.Current.CancellationToken);
    }

    private async Task<Guid> SystemRoleAsync(string key)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AccessDbContext>().Roles
            .Where(role => role.SystemKey == key).Select(role => role.Id).SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task AssignAsync(Guid userId, Guid roleId, string? scopeObjectId = null, DateTime? expiresAt = null)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        Assignment assignment = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = roleId,
            ScopeObjectId = scopeObjectId,
            ExpiresAt = expiresAt,
            CreatedAt = _clock.UtcNow,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class PersonCallContext(ClaimsPrincipal person) : ServerCallContext
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

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private sealed class AlwaysSignedIn : ISignedInSessions
    {
        public ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }
}
