using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

internal sealed class TestChurch
{
    public const string Music = "music";
    public const string Choir = "choir";

    public static readonly Permission PostNews = new("ministries.news.post", ScopeKind.Ministry, "Post news");
    public static readonly Permission ReadNews = new("ministries.news.read", ScopeKind.Ministry, "Read news");
    public static readonly Permission EditRoster = new("ministries.roster.edit", ScopeKind.Ministry, "Edit the roster");
    public static readonly Permission ManageAccounts = new("accounts.manage", ScopeKind.Global, "Manage accounts", isSensitive: true);
    private static readonly Permission ViewAccounts = new("accounts.view", ScopeKind.Global, "View accounts", isSensitive: true);
    public static readonly Permission ReceiveNotices = new("ministries.notices.receive", ScopeKind.Ministry, "Get notices");
    public static readonly Permission SeePictures = new("media.view", ScopeKind.Global, "See the media library");

    public static readonly RoleKit[] Kits = [new(RoleKits.Pictures, ["media.view"])];

    public static readonly SystemRole[] SystemRoles =
    [
        new(AccessService.OwnerKey, "Owner", "", ScopeKind.Global, ["*"]),
        new("access-manager", "Access manager", "", ScopeKind.Global, ["access.*", "accounts.view"]),
        new("account-manager", "Account manager", "", ScopeKind.Global, ["accounts.*"]),
        new("member", "Member", "", ScopeKind.Ministry, ["ministries.news.read"], AssignableBy: ["leader", "admin", "staff"]),
        new("leader", "Leader", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("admin", "Admin", "", ScopeKind.Ministry, ["ministries.*"], AssignableBy: ["staff"], AppliesToChildren: true),
        new("staff", "Staff", "", ScopeKind.Global, ["ministries.*"], AssignableBy: []),
        new("follower", "Follower", "", ScopeKind.Ministry, ["ministries.notices.receive"], PhoneHoldable: true),
        new("publicity", "Publicity", "", ScopeKind.Ministry, ["ministries.news.post"], AssignableBy: ["leader", "staff"], Kits: [RoleKits.Pictures]),
    ];

    public static readonly SystemRolePatterns[] Additions = [new("member", ["ministries.notices.receive"])];

    public static PermissionCatalog NewCatalog() =>
        new([new PermissionSet([.. AccessPermissions.All, PostNews, ReadNews, EditRoster, ManageAccounts, ViewAccounts, ReceiveNotices, SeePictures])]);

    private readonly TestClock _clock = new();
    private readonly List<Assignment> _assignments = [];

    public TestChurch()
    {
        List<SystemRole> declared = AccessReconciler.Declared(SystemRoles, Additions);
        Dictionary<string, Guid> ids = declared.ToDictionary(role => role.Key, _ => Guid.CreateVersion7());
        foreach (SystemRole role in declared)
        {
            Guid id = ids[role.Key];
            Roles[id] = new RoleRecord
            {
                Id = id,
                SystemKey = role.Key,
                Name = role.Name,
                ScopeKind = role.ScopeKind.Name,
                AppliesToChildren = role.AppliesToChildren,
                PhoneHoldable = role.PhoneHoldable,
                Patterns = AccessReconciler.PatternsFor(id, role, Kits.ToDictionary(kit => kit.Name)),
                Assigners = (role.AssignableBy ?? []).Select(key => new RoleAssigner { RoleId = id, AssignerRoleId = ids[key] }).ToList(),
            };
        }
    }

    public PermissionCatalog Catalog { get; } = NewCatalog();

    public IScopeHierarchy Hierarchy { get; } = new MusicOverChoir();

    public Dictionary<Guid, RoleRecord> Roles { get; } = [];

    public DateTime Now => _clock.UtcNow;

    public RoleRecord SystemRole(string key) => Roles.Values.Single(role => role.SystemKey == key);

    public Guid AddRole(string name, ScopeKind kind, string[] patterns, Guid[]? includes = null)
    {
        RoleRecord role = new() { Id = Guid.CreateVersion7(), Name = name, ScopeKind = kind.Name, CreatedAt = Now };
        role.Patterns.AddRange(patterns.Select(pattern => new RolePattern { RoleId = role.Id, Pattern = pattern }));
        role.Includes.AddRange((includes ?? []).Select(id => new RoleInclude { RoleId = role.Id, IncludedRoleId = id }));
        Roles[role.Id] = role;
        return role.Id;
    }

    public Assignment Assign(Guid userId, Guid roleId, string? scopeObjectId = null, DateTime? expiresAt = null)
    {
        Assignment assignment = new()
        {
            Id = Guid.CreateVersion7(), UserId = userId, RoleId = roleId, ScopeObjectId = scopeObjectId, ExpiresAt = expiresAt, CreatedAt = Now,
        };
        _assignments.Add(assignment);
        return assignment;
    }

    public List<Assignment> Of(Guid userId) => _assignments.Where(assignment => assignment.UserId == userId).ToList();

    public Task<GrantSet> GrantsAsync(Guid userId) =>
        RoleGrants.CompileAsync(Of(userId).Select(assignment => (assignment.RoleId, assignment.ScopeObjectId)), Roles, Hierarchy, TestContext.Current.CancellationToken);

    public async Task<string?> GiveAsync(Guid actorId, Guid userId, string roleKey, string objectId)
    {
        RoleRecord role = SystemRole(roleKey);
        Assigner actor = new(await GrantsAsync(actorId), false, Of(actorId));
        string? refusal = await new AssignmentPolicy(Hierarchy, Catalog).RefusalAsync(actor, role, objectId, Roles, TestContext.Current.CancellationToken);
        if (refusal is null)
            Assign(userId, role.Id, objectId);
        return refusal;
    }

    public sealed class MusicOverChoir : IScopeHierarchy
    {
        public ValueTask<IReadOnlyList<string>> ChildrenOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(objectId == Music ? [Choir] : []);

        public ValueTask<string?> ParentOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(objectId == Choir ? Music : null);
    }
}
