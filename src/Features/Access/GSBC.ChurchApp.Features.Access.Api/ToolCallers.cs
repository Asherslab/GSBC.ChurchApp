using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.AspNetCore.Http;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class ToolCallers(IHttpContextAccessor http)
{
    public bool ActsThroughATool(Guid userId) =>
        http.HttpContext?.User is { } caller && Caller.IsTool(caller) && Caller.UserId(caller) == userId;
}
