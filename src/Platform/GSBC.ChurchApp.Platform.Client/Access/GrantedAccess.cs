using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Access;

public sealed class GrantedAccess : IAccess
{
    private GrantSet _grants = GrantSet.Empty;

    public event Action? Changed;

    public bool Can(Permission permission, string? objectId = null) => _grants.IsAllowed(permission, objectId);

    public void Replace(GrantSet grants)
    {
        _grants = grants;
        Changed?.Invoke();
    }
}
