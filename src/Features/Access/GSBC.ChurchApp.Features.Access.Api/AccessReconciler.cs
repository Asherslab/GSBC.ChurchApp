using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessReconciler(
    IServiceScopeFactory scopes, PermissionCatalog catalog, IEnumerable<SystemRole> systemRoles, IEnumerable<SystemRolePatterns> additions,
    IEnumerable<RoleKit> kits, TimeProvider time, ILogger<AccessReconciler> logger) : BackgroundService
{
    private const long AdvisoryLockKey = 0x6773_6263_6163_6373;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await ReconcileAsync(scope.ServiceProvider.GetRequiredService<AccessDbContext>(), stoppingToken);
                await RemoveMadeHereRolesAsync(scope.ServiceProvider, stoppingToken);
                return;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Couldn't reconcile the permission catalogue and system roles yet; retrying");
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    public async Task ReconcileAsync(AccessDbContext db, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cancellationToken);

        Dictionary<string, CatalogEntry> stored = await db.Catalog.ToDictionaryAsync(entry => entry.Node, cancellationToken);
        foreach (var permission in catalog.ByNode.Values)
        {
            if (!stored.Remove(permission.Node, out CatalogEntry? entry))
                db.Catalog.Add(entry = new CatalogEntry { Node = permission.Node, ScopeKind = permission.ScopeKind.Name });
            entry.ScopeKind = permission.ScopeKind.Name;
            entry.Description = permission.Description;
            entry.Sensitive = permission.IsSensitive;
        }
        db.Catalog.RemoveRange(stored.Values);

        Dictionary<string, RoleRecord> roles = await db.Roles.Include(role => role.Patterns).Include(role => role.Assigners)
            .Where(role => role.SystemKey != null).ToDictionaryAsync(role => role.SystemKey!, cancellationToken);
        Dictionary<string, RoleKit> kitsByName = kits.ToDictionary(kit => kit.Name);
        Dictionary<string, RoleRecord> reconciled = [];
        List<(RoleRecord Role, IReadOnlyList<string> From)> starting = [];
        int position = 0;
        foreach (SystemRole declared in Declared(systemRoles, additions))
        {
            if (!roles.Remove(declared.Key, out RoleRecord? role)
                && (declared.Replaces ?? []).FirstOrDefault(roles.ContainsKey) is { } replaced
                && roles.Remove(replaced, out role))
            {
                role.SystemKey = declared.Key;
            }
            if (role is null)
            {
                db.Roles.Add(role = NewRole(declared));
                if (declared.StartsWithHoldersOf is { Count: > 0 } from)
                    starting.Add((role, from));
            }
            role.Name = declared.Name;
            role.Description = declared.Description;
            role.ScopeKind = declared.ScopeKind.Name;
            role.AppliesToChildren = declared.AppliesToChildren;
            role.PhoneHoldable = declared.PhoneHoldable;
            role.Feature = declared.Feature;
            role.Hidden = declared.Hidden;
            role.Position = position++;
            reconciled[declared.Key] = role;
            ReconcilePatterns(role, declared, kitsByName);
        }
        db.Roles.RemoveRange(roles.Values);
        await CopyHoldersAsync(db, starting, cancellationToken);

        foreach (SystemRole declared in systemRoles)
        {
            RoleRecord role = reconciled[declared.Key];
            HashSet<Guid> assigners = (declared.AssignableBy ?? [])
                .Select(key => reconciled.TryGetValue(key, out RoleRecord? assigner)
                    ? assigner.Id
                    : throw new InvalidOperationException($"The role '{declared.Key}' is assignable by '{key}', which no feature declares."))
                .ToHashSet();
            role.Assigners.RemoveAll(assigner => !assigners.Remove(assigner.AssignerRoleId));
            role.Assigners.AddRange(assigners.Select(id => new RoleAssigner { RoleId = role.Id, AssignerRoleId = id }));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
    }

    private RoleRecord NewRole(SystemRole declared) => new()
    {
        Id = Guid.CreateVersion7(),
        SystemKey = declared.Key,
        Name = declared.Name,
        ScopeKind = declared.ScopeKind.Name,
        CreatedAt = time.GetUtcNow().UtcDateTime,
    };

    public static List<RolePattern> PatternsFor(Guid roleId, SystemRole declared, IReadOnlyDictionary<string, RoleKit> kits) =>
    [
        .. declared.Patterns.Select(pattern => new RolePattern { RoleId = roleId, Pattern = pattern }),
        .. (declared.Kits ?? []).SelectMany(name => kits.TryGetValue(name, out RoleKit? kit)
                ? kit.Patterns
                : throw new InvalidOperationException($"The role '{declared.Key}' uses the kit '{name}', which no feature declares."))
            .Except(declared.Patterns)
            .Distinct()
            .Select(pattern => new RolePattern { RoleId = roleId, Pattern = pattern, Everywhere = declared.ScopeKind != ScopeKind.Global }),
    ];

    private static void ReconcilePatterns(RoleRecord role, SystemRole declared, IReadOnlyDictionary<string, RoleKit> kits)
    {
        List<RolePattern> wanted = PatternsFor(role.Id, declared, kits);
        role.Patterns.RemoveAll(existing => wanted.All(pattern => pattern.Pattern != existing.Pattern));
        foreach (RolePattern pattern in wanted)
        {
            if (role.Patterns.Find(existing => existing.Pattern == pattern.Pattern) is { } existing)
                existing.Everywhere = pattern.Everywhere;
            else
                role.Patterns.Add(pattern);
        }
    }

    private static async Task CopyHoldersAsync(AccessDbContext db, List<(RoleRecord Role, IReadOnlyList<string> From)> starting, CancellationToken cancellationToken)
    {
        foreach ((RoleRecord role, IReadOnlyList<string> from) in starting)
        {
            List<Assignment> held = await db.Assignments.AsNoTracking()
                .Where(assignment => assignment.Role!.SystemKey != null && from.Contains(assignment.Role.SystemKey))
                .ToListAsync(cancellationToken);
            db.Assignments.AddRange(held
                .DistinctBy(assignment => (assignment.UserId, assignment.ScopeObjectId))
                .Select(assignment => new Assignment
                {
                    Id = Guid.CreateVersion7(),
                    UserId = assignment.UserId,
                    RoleId = role.Id,
                    ScopeObjectId = assignment.ScopeObjectId,
                    Source = assignment.Source,
                    ExpiresAt = assignment.ExpiresAt,
                    CreatedAt = assignment.CreatedAt,
                    CreatedBy = assignment.CreatedBy,
                }));
        }
    }

    public async Task RemoveMadeHereRolesAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        AccessDbContext db = services.GetRequiredService<AccessDbContext>();
        List<IRoleUses> uses = services.GetServices<IRoleUses>().ToList();
        List<RoleRecord> madeHere = await db.Roles.Where(role => role.SystemKey == null).ToListAsync(cancellationToken);
        foreach (RoleRecord role in madeHere)
        {
            List<string> usedBy = [];
            foreach (IRoleUses use in uses)
                usedBy.AddRange(await use.UsesAsync(role.Id, cancellationToken));
            if (usedBy.Count == 0)
                db.Roles.Remove(role);
            else
                logger.LogWarning("Kept the made-here role {Role} because {Uses} is for people in it; aim it elsewhere and it goes", role.Name, string.Join(", ", usedBy));
        }
        if (madeHere.Count == 0)
            return;
        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
    }

    public static List<SystemRole> Declared(IEnumerable<SystemRole> declared, IEnumerable<SystemRolePatterns> added)
    {
        List<SystemRole> roles = declared.ToList();
        foreach (SystemRolePatterns addition in added)
        {
            int index = roles.FindIndex(role => role.Key == addition.RoleKey);
            if (index < 0)
                throw new InvalidOperationException($"Patterns are added to the role '{addition.RoleKey}', which no feature declares.");
            roles[index] = roles[index] with { Patterns = [.. roles[index].Patterns, .. addition.Patterns.Except(roles[index].Patterns)] };
        }
        return roles;
    }
}
