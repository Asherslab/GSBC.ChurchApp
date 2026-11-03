using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class GrantsFeed(AccessDbContext db, IScopeHierarchy hierarchy, TimeProvider time)
{
    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Assignment> active = await db.ActiveAssignments(now).ToListAsync(cancellationToken);
        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(active.Select(assignment => assignment.RoleId), cancellationToken);
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == AccessFeeds.Grants)
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);
        ILookup<string, Assignment> byUser = active.ToLookup(assignment => assignment.UserId.ToString());

        foreach (string userId in byUser.Select(group => group.Key).Union(published.Keys))
        {
            GrantSet grants = await RoleGrants.CompileAsync(
                byUser[userId].Select(assignment => (assignment.RoleId, assignment.ScopeObjectId)), roles, hierarchy, cancellationToken);
            byte[] payload = Encode(Messages(grants));
            FeedChangeRecord? before = published.GetValueOrDefault(userId);
            if (before is not null && before.Payload.AsSpan().SequenceEqual(payload))
                continue;

            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = AccessFeeds.Grants,
                RecordId = userId,
                ScopeId = userId,
                Version = (before?.Version ?? 0) + 1,
                Payload = payload,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static GrantList Messages(GrantSet grants) => new()
    {
        Grants = grants.Grants
            .Select(grant => new GrantMessage { Pattern = grant.Pattern.Text, ScopeKind = grant.Scope.Kind.Name, ObjectId = grant.Scope.ObjectId })
            .DistinctBy(grant => (grant.Pattern, grant.ScopeKind, grant.ObjectId))
            .OrderBy(grant => grant.Pattern, StringComparer.Ordinal)
            .ThenBy(grant => grant.ScopeKind, StringComparer.Ordinal)
            .ThenBy(grant => grant.ObjectId, StringComparer.Ordinal)
            .ToList(),
    };

    private static byte[] Encode(GrantList grants)
    {
        using MemoryStream stream = new();
        AccessContractsModel.Instance.Serialize(stream, grants);
        return stream.ToArray();
    }
}
