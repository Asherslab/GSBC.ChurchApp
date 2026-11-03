using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class HubTileTests : ConfigurationHost
{
    private const string Hostile = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" onload="alert(1)"><script>alert(2)</script><path d="M2 12 12 2 22 12z" fill="#2a3d5e"/></svg>""";
    private static readonly DateTime Now = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ATileComesBackFromADraftWithItsSizeColourIconAndAudienceAndAnUploadedIconCleaned()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Guid leaders = Guid.CreateVersion7();
        PatchMessage icon = Add(HubSurfaces.Icon.Surface, HubSurfaces.Area, null, (HubSurfaces.Icon.Name.Key, "Dove"), (HubSurfaces.Icon.Drawing.Key, Hostile));
        PatchMessage bulletin = Add(WaitingTiles.Bulletin.Surface, HubSurfaces.Area, null,
            (HubTileKeys.Columns, "2"), (HubTileKeys.Rows, "2"), (HubTileKeys.Tone, TileTones.Primary.Value), (HubTileKeys.Icon, IconSetting.Custom(icon.PlacementId)));
        bulletin.Audience = Audience.HoldingRole(leaders).Text;
        PatchMessage columns = Add(HubSurfaces.Grid.Surface, HubSurfaces.Area, null, (HubSurfaces.Grid.Columns.Key, "3"));

        List<Placement> live = await PublishNowAsync(staff, HubSurfaces.Area, null, icon, bulletin, columns);

        Placement drawn = live.Single(placement => placement.Id == icon.PlacementId);
        Assert.Equal(SvgIcons.Sanitise(Hostile), ValueOf(drawn, HubSurfaces.Icon.Drawing.Key));
        Assert.DoesNotContain("alert", ValueOf(drawn, HubSurfaces.Icon.Drawing.Key));

        IReadOnlyList<ArrangedSurface> arranged = SurfaceLayout.Arrange(
            new SurfaceCatalog([HubSurfaces.All, WaitingTiles.Surfaces]).In(HubSurfaces.Area), live.Select(placement => placement.ToStored()));
        ArrangedSurface tile = arranged.Single(entry => entry.Surface == WaitingTiles.Bulletin.Surface);
        TileLook look = TileLook.Of(WaitingTiles.Bulletin, tile.Values);
        Assert.Equal((2, 2, TileTones.Primary, IconSetting.Custom(icon.PlacementId)), (look.Columns, look.Rows, look.Tone, look.Icon));
        Assert.Equal("Read the bulletin", look.Title);
        Assert.Equal(3, HubSurfaces.Grid.ColumnsIn(arranged));
        Assert.Equal(leaders, tile.Audience.RoleId);
    }

    [Fact]
    public async Task ATileForARoleShowsOnlyToPeopleAccessSaysHoldItAndOnlyBetweenItsDates()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Guid youthLeaders = Guid.CreateVersion7();
        PatchMessage notices = Add(HubSurfaces.PlainTile.Surface, HubSurfaces.Area, null, (HubTileKeys.Title, "Leaders' notices"));
        notices.Audience = Audience.HoldingRole(youthLeaders).Text;
        PatchMessage christmas = Add(HubSurfaces.PlainTile.Surface, HubSurfaces.Area, null, (HubTileKeys.Title, "Christmas services"));
        christmas.SetsDates = true;
        christmas.StartsAt = Now.AddDays(20);
        christmas.EndsAt = Now.AddDays(52);

        List<StoredPlacement> live = (await PublishNowAsync(staff, HubSurfaces.Area, null, notices, christmas)).Select(placement => placement.ToStored()).ToList();

        GrantSet leader = new([new Grant(PermissionPattern.Parse(RoleMembership.NodeOf(youthLeaders)), Scope.Global)]);
        Viewer holder = new(true, node => leader.IsAllowed(HubSurfaces.Area.AudiencePermission(node)));
        Viewer member = new(true, node => GrantSet.Empty.IsAllowed(HubSurfaces.Area.AudiencePermission(node)));
        Assert.Equal(["Leaders' notices"], Titles(SurfaceLayout.ShownTo(HubSurfaces.All.Surfaces, live, holder, Now)));
        Assert.Empty(Titles(SurfaceLayout.ShownTo(HubSurfaces.All.Surfaces, live, member, Now)));
        Assert.Equal(["Christmas services"], Titles(SurfaceLayout.ShownTo(HubSurfaces.All.Surfaces, live, Viewer.Anonymous, Now.AddDays(25))));
    }

    [Theory]
    [InlineData(HubTileKeys.Link, "javascript:alert(1)")]
    [InlineData(HubTileKeys.Link, "http://example.com")]
    [InlineData(HubTileKeys.Icon, "custom:not-an-id")]
    [InlineData(HubTileKeys.Tone, "#ff0000")]
    [InlineData(HubTileKeys.Columns, "9")]
    public async Task ATileOutsideWhatTheThemeAndTheAppAllowIsRefused(string key, string value)
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Draft draft = await NewDraftAsync(staff);

        RpcException refused = await Refused(() => SaveAsync(staff, draft, Add(HubSurfaces.PlainTile.Surface, HubSurfaces.Area, null, (HubTileKeys.Title, "Camp"), (key, value))));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
    }

    private static List<string> Titles(IEnumerable<ArrangedSurface> shown) =>
        shown.Where(entry => entry.Surface == HubSurfaces.PlainTile.Surface).Select(entry => entry.Values.Get(HubSurfaces.PlainTile.Tile.Title)).ToList();
}
