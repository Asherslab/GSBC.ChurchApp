using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class AccessEvaluatorTests : IAsyncLifetime
{
    private static readonly Permission PostNews = new("ministries.news.post", ScopeKind.Ministry, "Post news");
    private static readonly Permission ReadNews = new("ministries.news.read", ScopeKind.Ministry, "Read news");
    private static readonly Permission EditRoster = new("ministries.roster.edit", ScopeKind.Ministry, "Edit the roster");
    private static readonly Permission ManageAccounts = new("accounts.manage", ScopeKind.Global, "Manage accounts", isSensitive: true);
    private static readonly Permission ViewAccounts = new("accounts.view", ScopeKind.Global, "View accounts", isSensitive: true);
    private static readonly Permission ReceiveNotices = new("ministries.notices.receive", ScopeKind.Ministry, "Get notices");

    private static readonly SystemRole[] SystemRoles =
    [
        new(AccessService.OwnerKey, "Owner", "", ScopeKind.Global, ["*"]),
        new("access-manager", "Access manager", "", ScopeKind.Global, ["access.*", "accounts.view"]),
        new("account-manager", "Account manager", "", ScopeKind.Global, ["accounts.*"]),
        new("member", "Member", "", ScopeKind.Ministry, ["ministries.news.read"], AssignableBy: ["leader", "admin", "staff"]),
        new("leader", "Leader", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("admin", "Admin", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("staff", "Staff", "", ScopeKind.Global, ["ministries.*"], AssignableBy: []),
        new("follower", "Follower", "", ScopeKind.Ministry, ["ministries.notices.receive"], PhoneHoldable: true),
    ];

    private const string Music = "music";
    private const string Choir = "choir";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;
    private PermissionCatalog _catalog = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _catalog = new PermissionCatalog([new PermissionSet([.. AccessPermissions.All, PostNews, ReadNews, EditRoster, ManageAccounts, ViewAccounts, ReceiveNotices])]);
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSingleton(TimeProvider.System)
            .AddSingleton(_catalog)
            .AddSingleton<GrantCache>()
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
            .AddScoped<AccessChanges>()
            .AddSingleton(new FeedRegistry([new FeedDefinition(AccessFeeds.Grants, FeedAudience.OwnRecords)]))
            .AddSingleton<FeedChangeBroadcaster>()
            .AddScoped<SyncService>()
            .AddSyncData()
            .AddAccessData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        await db.Database.MigrateAsync();
        await new AccessReconciler(_services.GetRequiredService<IServiceScopeFactory>(), _catalog, SystemRoles, TimeProvider.System,
            NullLogger<AccessReconciler>.Instance).ReconcileAsync(db, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ScopesIncludesDeniesAndExpiryDecideWhatSomeoneMayDo()
    {
        Guid member = await AddRoleAsync("Member", ScopeKind.Ministry, ["ministries.news.read"]);
        Guid leader = await AddRoleAsync("Leader", ScopeKind.Ministry, ["ministries.*", "-ministries.roster.edit"], includes: [member]);
        Guid person = Guid.CreateVersion7();
        await AssignAsync(person, leader, scopeObjectId: "42");
        await AssignAsync(person, await SystemRoleAsync(AccessService.OwnerKey), expiresAt: DateTime.UtcNow.AddDays(-1));

        GrantSet grants = await GrantsAsync(person);

        Assert.True(grants.IsAllowed(PostNews, "42"));
        Assert.True(grants.IsAllowed(ReadNews, "42"));
        Assert.False(grants.IsAllowed(EditRoster, "42"));
        Assert.False(grants.IsAllowed(PostNews, "7"));
        Assert.False(grants.IsAllowed(AccessPermissions.EditRoles));
    }

    [Fact]
    public async Task NobodyCanHandOutPermissionsTheyDoNotHold()
    {
        Guid manager = Guid.CreateVersion7();
        await AssignAsync(manager, await SystemRoleAsync("access-manager"));
        GrantSet actor = await GrantsAsync(manager);

        Assert.Contains(ManageAccounts, RoleGrants.NotHeld(actor, ["*"], ScopeKind.Global, null, _catalog));
        Assert.Equal([ManageAccounts], RoleGrants.NotHeld(actor, ["accounts.*"], ScopeKind.Global, null, _catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.*", "accounts.view"], ScopeKind.Global, null, _catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.assignments.edit"], ScopeKind.Ministry, "42", _catalog));
    }

    [Fact]
    public async Task TheLastActiveOwnerCannotBeRemoved()
    {
        Guid owner = await SystemRoleAsync(AccessService.OwnerKey);
        Assignment current = await AssignAsync(Guid.CreateVersion7(), owner);
        Assignment lapsed = await AssignAsync(Guid.CreateVersion7(), owner, expiresAt: DateTime.UtcNow.AddDays(-1));

        Assert.True(await IsLastActiveOwnerAsync(current));
        Assert.False(await IsLastActiveOwnerAsync(lapsed));

        Assignment second = await AssignAsync(Guid.CreateVersion7(), owner);

        Assert.False(await IsLastActiveOwnerAsync(current));
        Assert.False(await IsLastActiveOwnerAsync(second));
    }

    [Fact]
    public async Task APhoneFollowsWithoutAnAccountAndTheFollowMovesToThePersonWhoSignsIn()
    {
        Guid phone = Guid.CreateVersion7(), leader = Guid.CreateVersion7(), signsIn = Guid.CreateVersion7();
        await AssignAsync(leader, await SystemRoleAsync("leader"), scopeObjectId: Music);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IPhoneRoles phones = scope.ServiceProvider.GetRequiredService<IPhoneRoles>();
        IPermissionHolders holders = scope.ServiceProvider.GetRequiredService<IPermissionHolders>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await phones.GiveAsync(phone, "follower", Music, null, cancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await phones.GiveAsync(phone, "member", Music, null, cancellationToken));

        PermissionHolders music = await holders.HoldersAsync(ReceiveNotices, Music, cancellationToken);
        Assert.Equal([phone], music.Phones);
        Assert.Equal([leader], music.People);
        Assert.Empty((await holders.HoldersAsync(ReceiveNotices, "youth", cancellationToken)).Phones);

        await phones.MoveToPersonAsync(phone, signsIn, cancellationToken);

        PermissionHolders moved = await holders.HoldersAsync(ReceiveNotices, Music, cancellationToken);
        Assert.Empty(moved.Phones);
        Assert.Equal(new HashSet<Guid> { leader, signsIn }, moved.People.ToHashSet());
        Assert.Empty(await phones.HeldAsync(phone, cancellationToken));
    }

    private async Task<bool> IsLastActiveOwnerAsync(Assignment assignment)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        RoleRecord owner = await db.Roles.SingleAsync(role => role.Id == assignment.RoleId, TestContext.Current.CancellationToken);
        return await scope.ServiceProvider.GetRequiredService<AssignmentRules>()
            .LastOwnerAsync(owner, [assignment], TestContext.Current.CancellationToken) is not null;
    }

    [Fact]
    public async Task LeadersAddMembersButOnlyStaffMakeLeaders()
    {
        Guid staff = Guid.CreateVersion7(), musicLeader = Guid.CreateVersion7(), choirMember = Guid.CreateVersion7(), newcomer = Guid.CreateVersion7();
        await AssignAsync(staff, await SystemRoleAsync("staff"));
        await AssignAsync(choirMember, await SystemRoleAsync("member"), scopeObjectId: Choir);

        Assert.True((await GiveAsync(staff, musicLeader, "leader", Music)).Done);
        Assert.True((await GiveAsync(musicLeader, newcomer, "member", Choir)).Done);

        AssignmentOutcome leaderMakingLeader = await GiveAsync(musicLeader, newcomer, "leader", Choir);
        Assert.False(leaderMakingLeader.Done);
        Assert.Equal("Only someone with the Staff role can give the Leader role. Ask one of them.", leaderMakingLeader.Refusal);
        Assert.False((await GiveAsync(choirMember, newcomer, "member", Choir)).Done);
        Assert.False((await GiveAsync(musicLeader, newcomer, "member", "youth")).Done);
    }

    [Fact]
    public async Task NobodyGivesARoleThatDoesMoreThanTheyCan()
    {
        Guid accessManager = Guid.CreateVersion7(), someone = Guid.CreateVersion7();
        await AssignAsync(accessManager, await SystemRoleAsync("access-manager"));

        AssignmentOutcome outcome = await GiveAsync(accessManager, someone, "leader", Music);

        Assert.False(outcome.Done);
        Assert.StartsWith("You can't give the Leader role, because it lets people do things you can't do yourself: ", outcome.Refusal);
        Assert.Contains("Post news", outcome.Refusal);
    }

    [Fact]
    public async Task ALeaderLeadsTheMinistrysTeamsButAMemberIsOnlyAMemberWhereTheyJoined()
    {
        Guid leader = Guid.CreateVersion7(), member = Guid.CreateVersion7();
        await AssignAsync(leader, await SystemRoleAsync("leader"), scopeObjectId: Music);
        await AssignAsync(member, await SystemRoleAsync("member"), scopeObjectId: Music);

        GrantSet leads = await GrantsAsync(leader);
        GrantSet joined = await GrantsAsync(member);

        Assert.True(leads.IsAllowed(PostNews, Choir));
        Assert.False(leads.IsAllowed(PostNews, "youth"));
        Assert.True(joined.IsAllowed(ReadNews, Music));
        Assert.False(joined.IsAllowed(ReadNews, Choir));
    }

    [Fact]
    public async Task APreviewSaysWhoGainsAndLosesWhatBeforeARoleChanges()
    {
        Guid roleId = await AddRoleAsync("Roster helper", ScopeKind.Ministry, ["ministries.roster.edit"]);
        Guid helper = Guid.CreateVersion7();
        await AssignAsync(helper, roleId, scopeObjectId: Music);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        RoleRecord changed = await db.Roles.AsNoTracking().Include(role => role.Patterns).SingleAsync(role => role.Id == roleId, TestContext.Current.CancellationToken);
        changed.Patterns = [new RolePattern { RoleId = roleId, Pattern = "ministries.news.post" }];
        RoleEffect effect = await scope.ServiceProvider.GetRequiredService<AccessInsights>().PreviewAsync(changed, roleId, TestContext.Current.CancellationToken);

        PersonEffect person = Assert.Single(effect.People);
        Assert.Equal(helper.ToString(), person.UserId);
        Assert.Equal(("ministries.news.post", Music), Assert.Single(person.Gains.Select(change => (change.Node, change.ObjectId))));
        Assert.Equal(("ministries.roster.edit", Music), Assert.Single(person.Losses.Select(change => (change.Node, change.ObjectId))));
    }

    [Fact]
    public async Task WhatSomeoneCanDoIsGroupedByPlaceWithTheRoleThatGivesIt()
    {
        Guid person = Guid.CreateVersion7();
        await AssignAsync(person, await SystemRoleAsync("account-manager"));
        await AssignAsync(person, await SystemRoleAsync("leader"), scopeObjectId: Music);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        PersonAbilities abilities = await scope.ServiceProvider.GetRequiredService<AccessInsights>().ExplainAsync(person, TestContext.Current.CancellationToken);

        PlaceAbilities everywhere = Assert.Single(abilities.Places, place => place.ObjectId is null);
        Assert.Equal(["accounts.manage", "accounts.view"], everywhere.Nodes);
        Assert.Equal("Account manager", Assert.Single(everywhere.Because).RoleName);
        PlaceAbilities music = Assert.Single(abilities.Places, place => place.ObjectId == Music);
        Assert.True(music.IncludesTeams);
        Assert.Contains("ministries.news.post", music.Nodes);
        Assert.Equal("Leader", Assert.Single(music.Because).RoleName);
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
        Assert.DoesNotContain(Decode(leaders), grant => grant.ObjectId == Choir && grant.Pattern == "ministries.news.read");
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

    private sealed class MusicOverChoir : IScopeHierarchy
    {
        public ValueTask<IReadOnlyList<string>> ChildrenOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(objectId == Music ? [Choir] : []);

        public ValueTask<string?> ParentOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(objectId == Choir ? Music : null);
    }

    private async Task<GrantSet> GrantsAsync(Guid userId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AccessEvaluator>().GrantsForAsync(userId, TestContext.Current.CancellationToken);
    }

    private async Task<Guid> AddRoleAsync(string name, ScopeKind kind, string[] patterns, Guid[]? includes = null)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        RoleRecord role = new() { Id = Guid.CreateVersion7(), Name = name, ScopeKind = kind.Name, CreatedAt = DateTime.UtcNow };
        role.Patterns.AddRange(patterns.Select(pattern => new RolePattern { RoleId = role.Id, Pattern = pattern }));
        role.Includes.AddRange((includes ?? []).Select(id => new RoleInclude { RoleId = role.Id, IncludedRoleId = id }));
        db.Roles.Add(role);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return role.Id;
    }

    private async Task<Guid> SystemRoleAsync(string key)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AccessDbContext>().Roles
            .Where(role => role.SystemKey == key).Select(role => role.Id).SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Assignment> AssignAsync(Guid userId, Guid roleId, string? scopeObjectId = null, DateTime? expiresAt = null)
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
            CreatedAt = DateTime.UtcNow,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return assignment;
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
        await _postgres.DisposeAsync();
    }
}
