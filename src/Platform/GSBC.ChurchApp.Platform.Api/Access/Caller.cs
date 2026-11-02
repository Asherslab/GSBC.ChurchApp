using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Access;

public static class Caller
{
    public const string UserIdClaim = "sub";
    public const string SessionIdClaim = "sid";

    public static Guid? UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(UserIdClaim), out Guid id) ? id : null;

    public static string? SessionId(ClaimsPrincipal user) =>
        user.FindFirstValue(SessionIdClaim) is { Length: > 0 } session ? session : null;
}
