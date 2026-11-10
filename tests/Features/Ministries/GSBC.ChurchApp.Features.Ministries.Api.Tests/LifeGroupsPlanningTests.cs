using System.Reflection;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class LifeGroupsPlanningTests
{
    [Fact]
    public void PlanningTheLifeGroupsSundaysIsChurchWideAndLeadingAMinistryDoesNotGiveIt()
    {
        Scope lifeGroups = Scope.Of(ScopeKind.Ministry, Guid.CreateVersion7().ToString());
        GrantSet leader = new([.. MinistriesApiExtensions.LeaderPatterns.Select(pattern => new Grant(PermissionPattern.Parse(pattern), lifeGroups))]);
        GrantSet manager = new([new Grant(PermissionPattern.Parse("ministries.*"), Scope.Global)]);

        Assert.False(leader.IsAllowed(MinistriesPermissions.PlanLifeGroups));
        Assert.True(manager.IsAllowed(MinistriesPermissions.PlanLifeGroups));
        Assert.All([nameof(IMinistryLeadingService.SaveRoundAsync), nameof(IMinistryLeadingService.DeleteRoundAsync)], method =>
            Assert.Equal(MinistriesPermissions.PlanLifeGroupsNode, typeof(IMinistryLeadingService).GetMethod(method)!.GetCustomAttribute<RequiresAttribute>()!.Node));
        Assert.False(typeof(IScopedRequest).IsAssignableFrom(typeof(RoundSave)));
    }
}
