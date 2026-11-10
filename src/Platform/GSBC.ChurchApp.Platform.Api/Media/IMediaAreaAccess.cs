using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaAreaAccess
{
    string Area { get; }

    Task<bool> MayUploadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken);

    Task<bool> MayReadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken);
}
