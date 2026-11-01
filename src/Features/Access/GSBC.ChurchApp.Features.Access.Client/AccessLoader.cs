using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Client;

internal sealed class AccessLoader(AccessClient access, GrantedAccess granted, SessionSignal signal) : IClientStartupTask
{
    public Task RunAsync()
    {
        signal.Changed += ReloadAsync;
        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            granted.Replace(ToGrantSet(await access.GetMineAsync()));
        }
        catch (RpcException)
        {
            granted.Replace(GrantSet.Empty);
        }
    }

    private static GrantSet ToGrantSet(GrantList list) =>
        new(list.Grants
            .Select(grant => PermissionPattern.TryParse(grant.Pattern, out PermissionPattern? pattern) && ScopeKind.TryParse(grant.ScopeKind, out ScopeKind kind)
                ? new Grant(pattern, kind == ScopeKind.Global ? Scope.Global : Scope.Of(kind, grant.ObjectId ?? ""))
                : (Grant?)null)
            .OfType<Grant>()
            .ToList());
}
