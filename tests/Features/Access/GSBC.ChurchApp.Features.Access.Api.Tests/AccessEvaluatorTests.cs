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
    ];

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
            .AddScoped<AccessEvaluator>()
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

        Assert.Contains("accounts.manage", RoleGrants.NotHeld(actor, ["*"], ScopeKind.Global, null, _catalog));
        Assert.Equal(["accounts.manage"], RoleGrants.NotHeld(actor, ["accounts.*"], ScopeKind.Global, null, _catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.*", "accounts.view"], ScopeKind.Global, null, _catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.assignments.edit"], ScopeKind.Ministry, "42", _catalog));
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
