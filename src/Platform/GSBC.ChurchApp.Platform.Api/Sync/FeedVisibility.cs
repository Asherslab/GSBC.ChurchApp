using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public static class FeedVisibility
{
    public static async Task<bool> CanSeeFeedAsync(FeedDefinition feed, ClaimsPrincipal user, IAccessEvaluator access, CancellationToken cancellationToken) =>
        feed.Audience switch
        {
            FeedAudience.PublicAudience => true,
            FeedAudience.SignedInAudience or FeedAudience.OwnRecordsAudience or FeedAudience.EachRowAudience => IsSignedIn(user),
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
        if (feed.Audience is FeedAudience.EachRowAudience)
            return await RowsForTheirAudienceAsync(rows, user, access, cancellationToken);
        if (feed.Audience is not FeedAudience.PermissionAudience audience)
            return rows;

        Dictionary<string, bool> allowedByScope = [];
        foreach (string? scopeId in rows.Select(row => row.ScopeId).Distinct())
            allowedByScope[scopeId ?? ""] = await access.IsAllowedAsync(user, audience.Permission.Node, scopeId, cancellationToken);

        return rows.Where(row => allowedByScope[row.ScopeId ?? ""]).ToList();
    }

    private static async Task<List<FeedChangeRecord>> RowsForTheirAudienceAsync(
        List<FeedChangeRecord> rows, ClaimsPrincipal user, IAccessEvaluator access, CancellationToken cancellationToken)
    {
        GrantSet grants = Caller.UserId(user) is { } userId ? await access.GrantsForAsync(userId, cancellationToken) : GrantSet.Empty;
        Viewer viewer = new(IsSignedIn(user), node => PermissionNode.IsValid(node) && grants.IsAllowed(new Permission(node, ScopeKind.Global, node)));
        return rows.Select(row => row.Deleted || (row.ScopeId is { } text && Audience.TryParse(text, out Audience? audience) && audience.Includes(viewer)) ? row : Gone(row))
            .ToList();
    }

    private static FeedChangeRecord Gone(FeedChangeRecord row) =>
        new() { Cursor = row.Cursor, Feed = row.Feed, RecordId = row.RecordId, Version = row.Version, Deleted = true };

    private static bool IsSignedIn(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;
}
