using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Client;

internal sealed class PhoneAccessLoader(
    ILocalFeeds feeds, NativeBridge bridge, GrantedAccess granted, SessionSignal signal, AccessLoader online)
    : IClientStartupTask
{
    public Task RunAsync()
    {
        if (!feeds.IsAvailable)
            return online.RunAsync();

        signal.Changed += ReloadAsync;
        feeds.FeedsChanged += changed =>
        {
            if (changed.Contains(AccessFeeds.Grants))
                _ = ReloadAsync();
        };
        return ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        IReadOnlyList<StoredRecord> mine = bridge.SignedIn ? await feeds.ReadPageAsync(new FeedPageQuery(AccessFeeds.Grants, Limit: 1)) : [];
        GrantSet grants = mine.Count == 0
            ? GrantSet.Empty
            : AccessLoader.ToGrantSet(AccessContractsModel.Instance.Deserialize<GrantList>(new MemoryStream(mine[0].Payload)));
        granted.Replace(grants, bridge.SignedIn);
    }
}
