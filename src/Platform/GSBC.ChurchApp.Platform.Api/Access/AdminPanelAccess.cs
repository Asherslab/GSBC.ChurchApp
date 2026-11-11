using System.Security.Claims;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public sealed class AdminPanelAccess(PermissionCatalog catalog, IAccessEvaluator access)
{
    public async ValueTask<bool> MayUseAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (Caller.UserId(user) is not { } person)
            return false;
        GrantSet grants = await access.GrantsForAsync(person, cancellationToken);
        return catalog.OpeningAdminAreas.Any(grants.IsAllowedSomewhere);
    }
}
