using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Access;

public sealed class GrantedAccess : IAccess
{
    private GrantSet _grants = GrantSet.Empty;

    public event Action? Changed;

    public bool SignedIn { get; private set; }

    public bool Can(Permission permission, string? objectId = null) => _grants.IsAllowed(permission, objectId);

    public bool CanSomewhere(Permission permission) => _grants.IsAllowedSomewhere(permission);

    public void Replace(GrantSet grants, bool signedIn)
    {
        _grants = grants;
        SignedIn = signedIn;
        Changed?.Invoke();
    }
}
