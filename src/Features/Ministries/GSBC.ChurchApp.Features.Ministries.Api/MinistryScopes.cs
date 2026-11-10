using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryHierarchy(IServiceScopeFactory scopes) : IScopeHierarchy
{
    public async ValueTask<IReadOnlyList<string>> ChildrenOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken)
    {
        if (kind != ScopeKind.Ministry || !Guid.TryParse(objectId, out Guid id))
            return [];
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        return (await db.Ministries.AsNoTracking().Where(ministry => ministry.ParentId == id).Select(ministry => ministry.Id).ToListAsync(cancellationToken))
            .Select(child => child.ToString())
            .ToList();
    }

    public async ValueTask<string?> ParentOfAsync(ScopeKind kind, string objectId, CancellationToken cancellationToken)
    {
        if (kind != ScopeKind.Ministry || !Guid.TryParse(objectId, out Guid id))
            return null;
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        return (await db.Ministries.AsNoTracking().Where(ministry => ministry.Id == id).Select(ministry => ministry.ParentId).SingleOrDefaultAsync(cancellationToken))?.ToString();
    }
}

internal sealed class MinistryScopeObjects(MinistriesDbContext db) : IScopeObjects
{
    public ScopeKind Kind => ScopeKind.Ministry;

    public async ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid id) && await db.Ministries.AnyAsync(ministry => ministry.Id == id, cancellationToken);
}
