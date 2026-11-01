using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Access;

public interface IAccess
{
    event Action? Changed;

    bool Can(Permission permission, string? objectId = null);
}
