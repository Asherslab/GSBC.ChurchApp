using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Access;

public interface IScopeObjects
{
    ScopeKind Kind { get; }

    ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken);
}
