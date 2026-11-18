using System.Reflection;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Tiers;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void EverythingButReadingTheRegionsAndOverviewNeedsMapAreasManage()
    {
        IEnumerable<MethodInfo> managing = typeof(IOutreachMapsService).GetMethods().Where(method => method.Name is not (nameof(IOutreachMapsService.RegionsAsync) or nameof(IOutreachMapsService.OverviewAsync)));

        Assert.All(managing, method => Assert.Equal(OutreachMapsPermissions.ManageNode, method.GetCustomAttribute<RequiresAttribute>()?.Node));
    }
}
