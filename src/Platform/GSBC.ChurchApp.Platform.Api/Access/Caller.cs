using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Access;

public static class Caller
{
    public const string UserIdClaim = "sub";

    public static Guid? UserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(UserIdClaim), out Guid id) ? id : null;
}
