using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public static class FeedVisibility
{
    public static async Task<bool> CanSeeFeedAsync(FeedDefinition feed, ClaimsPrincipal user, IAccessEvaluator access, CancellationToken cancellationToken) =>
        feed.Audience switch
        {
            FeedAudience.PublicAudience => true,
            FeedAudience.SignedInAudience or FeedAudience.OwnRecordsAudience => IsSignedIn(user),
            FeedAudience.PermissionAudience audience when audience.Permission.ScopeKind == ScopeKind.Global =>
                IsSignedIn(user) && await access.IsAllowedAsync(user, audience.Permission.Node, null, cancellationToken),
            FeedAudience.PermissionAudience => IsSignedIn(user),
            _ => false,
        };

    public static async Task<List<FeedChangeRecord>> VisibleRowsAsync(
        FeedDefinition feed, List<FeedChangeRecord> rows, ClaimsPrincipal user, IAccessEvaluator access, CancellationToken cancellationToken)
    {
        if (feed.Audience is FeedAudience.OwnRecordsAudience)
            return Caller.UserId(user)?.ToString() is { } userId ? rows.Where(row => row.ScopeId == userId).ToList() : [];
        if (feed.Audience is not FeedAudience.PermissionAudience audience)
            return rows;

        Dictionary<string, bool> allowedByScope = [];
        foreach (string? scopeId in rows.Select(row => row.ScopeId).Distinct())
            allowedByScope[scopeId ?? ""] = await access.IsAllowedAsync(user, audience.Permission.Node, scopeId, cancellationToken);

        return rows.Where(row => allowedByScope[row.ScopeId ?? ""]).ToList();
    }

    private static bool IsSignedIn(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
