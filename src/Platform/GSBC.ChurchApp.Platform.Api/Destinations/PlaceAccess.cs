using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Api.Destinations;

public sealed class PlaceAccess(IAccessEvaluator access)
{
    public async Task<bool> MayOpenAsync(ClaimsPrincipal user, Audience audience, string? scope, CancellationToken cancellationToken)
    {
        if (audience == Audience.Everyone)
            return true;
        if (Caller.UserId(user) is null)
            return false;
        return audience.Node is not { } node || await access.IsAllowedAsync(user, node, scope, cancellationToken);
    }
}
