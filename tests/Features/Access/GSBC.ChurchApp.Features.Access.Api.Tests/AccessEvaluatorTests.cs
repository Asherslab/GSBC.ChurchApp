using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class AccessEvaluatorTests : IAsyncLifetime
{
    private static readonly Permission PostNews = new("ministries.news.post", ScopeKind.Ministry, "Post news");
    private static readonly Permission ReadNews = new("ministries.news.read", ScopeKind.Ministry, "Read news");
    private static readonly Permission EditRoster = new("ministries.roster.edit", ScopeKind.Ministry, "Edit the roster");
    private static readonly Permission ManageAccounts = new("accounts.manage", ScopeKind.Global, "Manage accounts", isSensitive: true);
    private static readonly Permission ViewAccounts = new("accounts.view", ScopeKind.Global, "View accounts", isSensitive: true);

    private static readonly SystemRole[] SystemRoles =
    [
        new(AccessService.OwnerKey, "Owner", "", ScopeKind.Global, ["*"]),
        new("access-manager", "Access manager", "", ScopeKind.Global, ["access.*", "accounts.view"]),
        new("account-manager", "Account manager", "", ScopeKind.Global, ["accounts.*"]),
        new("member", "Member", "", ScopeKind.Ministry, ["ministries.news.read"], AssignableBy: ["leader", "admin", "staff"]),
        new("leader", "Leader", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("admin", "Admin", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("staff", "Staff", "", ScopeKind.Global, ["ministries.*"], AssignableBy: []),
    ];

    private const string Music = "music";
    private const string Choir = "choir";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;
    private PermissionCatalog _catalog = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _catalog = new PermissionCatalog([new PermissionSet([.. AccessPermissions.All, PostNews, ReadNews, EditRoster, ManageAccounts, ViewAccounts])]);
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
            .AddScoped<IRoleAssignments, RoleAssignments>()
            .AddAccessData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
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

    private async Task AssignAsync(Guid userId, Guid roleId, string? scopeObjectId = null, DateTime? expiresAt = null)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccessDbContext db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        db.Assignments.Add(new Assignment
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = roleId,
            ScopeObjectId = scopeObjectId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
