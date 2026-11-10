using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Client.Online;

namespace GSBC.ChurchApp.Features.Access.Client;

[AdminOnly]
internal sealed class AccessLoader(AccessClient access, GrantedAccess granted, SessionSignal signal, IFeedSignals feeds) : IClientStartupTask
{
    public Task RunAsync()
    {
        signal.Changed += ReloadAsync;
        feeds.FeedsChanged += changed =>
        {
            if (changed.Contains(AccessFeeds.Grants))
                _ = ReloadAsync();
        };
        feeds.Follow([AccessFeeds.Grants]);
        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            granted.Replace(GrantLists.ToGrantSet(await access.GetMineAsync()), signedIn: true);
        }
        catch (RpcException)
        {
            granted.Replace(GrantSet.Empty, signedIn: false);
        }
    }
}
