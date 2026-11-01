using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Access;

internal sealed class NoGrantsAccessEvaluator : IAccessEvaluator
{
    public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);
}
