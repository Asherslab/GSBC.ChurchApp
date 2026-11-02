using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessReconciler(
    IServiceScopeFactory scopes, PermissionCatalog catalog, IEnumerable<SystemRole> systemRoles, TimeProvider time,
    ILogger<AccessReconciler> logger) : BackgroundService
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
        Dictionary<string, RoleRecord> reconciled = [];
        foreach (SystemRole declared in systemRoles)
        {
            if (!roles.Remove(declared.Key, out RoleRecord? role))
            {
                db.Roles.Add(role = new RoleRecord
                {
                    Id = Guid.CreateVersion7(),
                    SystemKey = declared.Key,
                    Name = declared.Name,
                    ScopeKind = declared.ScopeKind.Name,
                    CreatedAt = time.GetUtcNow().UtcDateTime,
                });
            }
            role.Name = declared.Name;
            role.Description = declared.Description;
            role.ScopeKind = declared.ScopeKind.Name;
            role.AppliesToChildren = declared.AppliesToChildren;
            reconciled[declared.Key] = role;
            role.Patterns.RemoveAll(pattern => !declared.Patterns.Contains(pattern.Pattern));
            foreach (string pattern in declared.Patterns.Where(pattern => role.Patterns.All(existing => existing.Pattern != pattern)))
                role.Patterns.Add(new RolePattern { RoleId = role.Id, Pattern = pattern });
        }
        db.Roles.RemoveRange(roles.Values);

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
}
