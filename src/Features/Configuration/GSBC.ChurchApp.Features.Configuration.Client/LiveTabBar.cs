using GSBC.ChurchApp.Features.Configuration.Contracts;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Ui.Tabs;

namespace GSBC.ChurchApp.Features.Configuration.Client;

[AdminOnly]
public sealed class LiveTabBar(ConfigurationClient configuration, SurfaceCatalog surfaces, IAccess access, TimeProvider time) : ITabBarNow
{
    public async Task<IReadOnlyList<AppTab>> CurrentAsync()
    {
        if (!access.Can(TabBarSurfaces.Area.EditPermission))
            return TabBar.Starting;
        try
        {
            AreaPlacements live = await configuration.GetAreaAsync(TabBarSurfaces.Area, null);
            DateTime now = time.GetUtcNow().UtcDateTime;
            IReadOnlyList<ArrangedSurface> arranged = SurfaceLayout.Arrange(surfaces.In(TabBarSurfaces.Area), live.Placements.Select(placement => placement.ToStored()));
            return TabBar.For(arranged.Where(entry => entry.IsShownTo(Viewer.Anonymous, now)));
        }
        catch (RpcException)
        {
            return TabBar.Starting;
        }
    }
}
