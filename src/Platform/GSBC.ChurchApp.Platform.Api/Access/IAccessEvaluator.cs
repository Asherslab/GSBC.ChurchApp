using System.Security.Claims;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IAccessEvaluator
{
    ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken);

    ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken);
}
