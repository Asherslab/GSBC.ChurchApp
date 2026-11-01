using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class SessionService : ISessionService
{
    public ValueTask<CurrentSession> GetCurrentAsync(GetCurrentSessionRequest request, CallContext context = default)
    {
        ClaimsPrincipal? user = context.ServerCallContext?.GetHttpContext().User;

        return ValueTask.FromResult(user?.Identity?.IsAuthenticated == true
            ? new CurrentSession
            {
                SignedIn = true,
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                DisplayName = user.FindFirstValue(ClaimTypes.Name),
            }
            : new CurrentSession());
    }
}
