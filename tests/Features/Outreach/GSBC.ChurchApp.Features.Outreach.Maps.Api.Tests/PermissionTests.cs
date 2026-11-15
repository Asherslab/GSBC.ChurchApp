using System.Reflection;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Tiers;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class PermissionTests
{
    [Fact]
    public void EverythingButReadingTheRegionsNeedsMapAreasManage()
    {
        IEnumerable<MethodInfo> managing = typeof(IOutreachMapsService).GetMethods().Where(method => method.Name != nameof(IOutreachMapsService.RegionsAsync));

        Assert.All(managing, method => Assert.Equal(OutreachMapsPermissions.ManageNode, method.GetCustomAttribute<RequiresAttribute>()?.Node));
    }
}
