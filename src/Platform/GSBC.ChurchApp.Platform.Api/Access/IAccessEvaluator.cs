using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IAccessEvaluator
{
    ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken);
}
