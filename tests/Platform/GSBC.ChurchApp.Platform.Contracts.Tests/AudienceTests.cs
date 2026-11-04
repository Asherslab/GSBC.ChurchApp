using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class AudienceTests
{
    private static readonly Guid Deacons = Guid.Parse("0f8a3f8e-2d1b-4c55-9a52-1c1f0b8c0001");
    private static readonly Guid YouthLeaders = Guid.Parse("0f8a3f8e-2d1b-4c55-9a52-1c1f0b8c0002");

    [Fact]
    public void A_role_audience_includes_holders_of_any_of_its_roles_and_survives_a_round_trip()
    {
        Audience roles = Audience.HoldingAnyRole([Deacons, YouthLeaders]);

        Assert.True(Audience.TryParse(roles.Text, out Audience? parsed));
        Assert.Equal(roles, parsed);
        Assert.Equal([Deacons, YouthLeaders], parsed.RoleIds);
        Assert.True(parsed.Includes(new Viewer(true, node => node == RoleMembership.NodeOf(YouthLeaders))));
        Assert.False(parsed.Includes(new Viewer(true, _ => false)));
        Assert.False(parsed.Includes(Viewer.Anonymous));
    }

    [Fact]
    public void A_single_role_audience_saved_before_several_were_allowed_still_reads()
    {
        Assert.True(Audience.TryParse($"role:{Deacons:D}", out Audience? parsed));
        Assert.Equal([Deacons], parsed.RoleIds);
        Assert.False(Audience.TryParse($"role:{Deacons:D},not-a-role", out _));
    }
}
