using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class HubTileTests
{
    private const string Hostile = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" onload="alert(1)"><script>alert(2)</script><path d="M2 12 12 2 22 12z" fill="#2a3d5e"/></svg>""";
    private static readonly DateTime Now = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);
    private readonly PlacementRules _rules = new(TestSurfaces.Catalog, TestSurfaces.Permissions, new Library());

    [Fact]
    public async Task ATileKeepsItsSizeColourIconAndAudienceAndAnUploadedIconIsCleaned()
    {
        Guid leaders = Guid.CreateVersion7();
        Guid iconId = Guid.CreateVersion7();
        StoredPlacement icon = await PlacedAsync(HubSurfaces.Icon.Surface, iconId, (HubSurfaces.Icon.Name.Key, "Dove"), (HubSurfaces.Icon.Drawing.Key, Hostile));
        StoredPlacement bulletin = await PlacedAsync(WaitingTiles.Bulletin.Surface, Guid.CreateVersion7(), Audience.HoldingRole(leaders).Text,
            (HubTileKeys.Columns, "2"), (HubTileKeys.Rows, "2"), (HubTileKeys.Tone, TileTones.Primary.Value), (HubTileKeys.Icon, IconSetting.Custom(iconId.ToString())));
        StoredPlacement columns = await PlacedAsync(HubSurfaces.Grid.Surface, Guid.CreateVersion7(), (HubSurfaces.Grid.Columns.Key, "3"));

        Assert.Equal(SvgIcons.Sanitise(Hostile), icon.Values[HubSurfaces.Icon.Drawing.Key]);
        Assert.DoesNotContain("alert", icon.Values[HubSurfaces.Icon.Drawing.Key]);

        IReadOnlyList<ArrangedSurface> arranged = SurfaceLayout.Arrange(
            new SurfaceCatalog([HubSurfaces.All, WaitingTiles.Surfaces]).In(HubSurfaces.Area), [icon, bulletin, columns]);
        ArrangedSurface tile = arranged.Single(entry => entry.Surface == WaitingTiles.Bulletin.Surface);
        TileLook look = TileLook.Of(WaitingTiles.Bulletin, tile.Values);
        Assert.Equal((2, 2, TileTones.Primary, IconSetting.Custom(iconId.ToString())), (look.Columns, look.Rows, look.Tone, look.Icon));
        Assert.Equal("Read the bulletin", look.Title);
        Assert.Equal(3, HubSurfaces.Grid.ColumnsIn(arranged));
        Assert.Equal([leaders], tile.Audience.RoleIds);
    }

    [Fact]
    public async Task ATileForARoleShowsOnlyToPeopleAccessSaysHoldItAndOnlyBetweenItsDates()
    {
        Guid youthLeaders = Guid.CreateVersion7();
        StoredPlacement notices = await PlacedAsync(HubSurfaces.PlainTile.Surface, Guid.CreateVersion7(), Audience.HoldingRole(youthLeaders).Text,
            (HubTileKeys.Title, "Leaders' notices"));
        StoredPlacement christmas = (await PlacedAsync(HubSurfaces.PlainTile.Surface, Guid.CreateVersion7(), (HubTileKeys.Title, "Christmas services")))
            with { StartsAt = Now.AddDays(20), EndsAt = Now.AddDays(52) };
        List<StoredPlacement> live = [notices, christmas];

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
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
            PlacedAsync(HubSurfaces.PlainTile.Surface, Guid.CreateVersion7(), (HubTileKeys.Title, "Camp"), (key, value)));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
    }

    private Task<StoredPlacement> PlacedAsync(Surface surface, Guid id, params (string Key, string Value)[] values) => PlacedAsync(surface, id, "", values);

    private async Task<StoredPlacement> PlacedAsync(Surface surface, Guid id, string audience, params (string Key, string Value)[] values)
    {
        PlacementState state = new(surface.Key, 0, false, audience, null, null, new(values.ToDictionary(value => value.Key, value => value.Value)));
        return (await _rules.ValidatedAsync(surface, state, TestContext.Current.CancellationToken)).ToStored(id, 1);
    }

    private static List<string> Titles(IEnumerable<ArrangedSurface> shown) =>
        shown.Where(entry => entry.Surface == HubSurfaces.PlainTile.Surface).Select(entry => entry.Values.Get(HubSurfaces.PlainTile.Tile.Title)).ToList();
}
