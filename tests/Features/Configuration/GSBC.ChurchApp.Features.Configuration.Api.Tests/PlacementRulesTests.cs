using Grpc.Core;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class PlacementRulesTests
{
    private static readonly (string, string) Opens = (HubSurfaces.LinkTile.Link.Key, "https://www.biblegateway.com");
    private readonly Library _library = new();

    [Fact]
    public void OnlyHoldersOfTheAreasPermissionAtItsScopeCanArrangeIt()
    {
        GrantSet leader = Holding((TestSurfaces.EditModules, "42"));

        Assert.True(May(leader, TestSurfaces.Modules.Key, "42", publishing: false));
        Assert.False(May(leader, TestSurfaces.Modules.Key, "7", publishing: false));
        Assert.False(May(leader, HubSurfaces.Area.Key, null, publishing: false));
    }

    [Fact]
    public void PublishingNeedsThePublishPermissionForTheAreaItTouches()
    {
        GrantSet editor = Holding((HubSurfaces.Edit, null), (AppearanceSurfaces.Edit, null), (HubSurfaces.Publish, null));

        Assert.True(May(editor, HubSurfaces.Area.Key, null, publishing: true));
        Assert.True(May(editor, AppearanceSurfaces.Area.Key, null, publishing: false));
        Assert.False(May(editor, AppearanceSurfaces.Area.Key, null, publishing: true));
    }

    [Fact]
    public async Task EveryValueIsCheckedAgainstItsDescriptor()
    {
        DateTime starts = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        PlacementState tile = State(HubSurfaces.LinkTile.Surface, (HubSurfaces.LinkTile.Title.Key, "Bible"), (HubSurfaces.LinkTile.Link.Key, "http://example.org"), ("font", "Comic Sans"))
            with { Audience = "permission:nobody.declared.this", StartsAt = starts, EndsAt = starts.AddDays(-1) };

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => ValidatedAsync(tile));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Contains("https://", refused.Status.Detail);
        Assert.Contains("'font'", refused.Status.Detail);
        Assert.Contains("nobody.declared.this", refused.Status.Detail);
        Assert.Contains("end after it starts", refused.Status.Detail);
    }

    [Fact]
    public async Task FormattedTextWithAnUnsafeLinkIsRefused()
    {
        PlacementState welcome = State(TestSurfaces.Welcome, (TestSurfaces.About.Key, ContentJson.Write(new RichText { Markdown = "[Click me](javascript:alert(1))" })));

        Assert.Equal(StatusCode.InvalidArgument, (await Assert.ThrowsAsync<RpcException>(() => ValidatedAsync(welcome))).StatusCode);
    }

    [Fact]
    public void ASecondCopyOfASurfaceThatIsPlacedOnceIsRefused()
    {
        RpcException refused = Assert.Throws<RpcException>(() => PlacementRules.RefuseSecondCopy(TestSurfaces.News, [TestSurfaces.News.Key]));

        Assert.Equal(StatusCode.Aborted, refused.StatusCode);
        PlacementRules.RefuseSecondCopy(HubSurfaces.LinkTile.Surface, [HubSurfaces.LinkTile.Surface.Key]);
    }

    [Fact]
    public async Task AnImageInASettingIsStoredAsTheLibraryHasItNotAsClaimed()
    {
        MediaRef photo = _library.Add(new MediaRef { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg", Width = 800, Height = 600 });
        PlacementState tile = State(HubSurfaces.LinkTile.Surface, (HubSurfaces.LinkTile.Title.Key, "Camp"), Opens,
            Image(new MediaRef { Id = photo.Id, Key = photo.Key, ContentType = "image/jpeg", Url = "https://attacker.example/x.jpg" }));

        PlacementState stored = await ValidatedAsync(tile);

        Assert.True(HubSurfaces.LinkTile.Image.TryParse(stored.Values[HubSurfaces.LinkTile.Image.Key], out MediaRef? image));
        Assert.Equal((photo.Url, 800), (image!.Url, image.Width));
    }

    [Fact]
    public async Task APlacedPictureTakesTheLibrarysCurrentSizesWhenMediaAsks()
    {
        MediaRef photo = _library.Add(new MediaRef { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg", Width = 4000, Height = 2250 });
        PlacementState placed = await ValidatedAsync(State(HubSurfaces.LinkTile.Surface, (HubSurfaces.LinkTile.Title.Key, "Camp"), Opens, Image(photo)));
        MediaRef withoutOriginal = _library.Replace(new MediaRef
        {
            Id = photo.Id, Key = $"{new string('c', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/c.jpg", Width = 1280, Height = 720,
            Version = MediaSizeName.ForPhones,
            Sizes = [new MediaSize { Name = MediaSizeName.Thumbnail, Key = $"{new string('d', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/d.jpg", Width = 480, Height = 270 }],
        });

        PlacementState current = await Rules.WithCurrentMediaAsync(HubSurfaces.LinkTile.Surface, placed, TestContext.Current.CancellationToken);

        Assert.True(HubSurfaces.LinkTile.Image.TryParse(current.Values[HubSurfaces.LinkTile.Image.Key], out MediaRef? shown));
        Assert.Equal((withoutOriginal.Key, MediaSizeName.ForPhones), (shown!.Key, shown.Version));
        Assert.Equal(withoutOriginal.Sizes[0].Key, shown.At(MediaSizeName.Thumbnail).Key);
        Assert.False(PlacementState.Same(placed, current));
    }

    [Theory]
    [InlineData("https://baptist.com.au/give", "not a web page")]
    [InlineData("/more", "always the last tab")]
    [InlineData("/prayer-wall", "doesn't have")]
    [InlineData("/files/123", "opens outside the app")]
    public async Task ATabOpensAPlaceInsideTheAppOtherThanMore(string opens, string why)
    {
        PlacementState tab = State(TabBarSurfaces.Tab, (TabSettings.Opens.Key, opens), (TabSettings.Word.Key, "Give"), (TabSettings.Icon.Key, "heart"));

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => ValidatedAsync(tab));

        Assert.Contains(why, refused.Status.Detail);
    }

    [Fact]
    public async Task ATabMayOpenAPageOrOneThingAndTheStartingTabsNeedNothingSet()
    {
        await ValidatedAsync(State(TabBarSurfaces.Tab, (TabSettings.Opens.Key, "/series/advent"), (TabSettings.Word.Key, "Advent"), (TabSettings.Icon.Key, "sermons")));
        await ValidatedAsync(State(TabBarSurfaces.Home));
        await ValidatedAsync(State(TabBarSurfaces.Sermons) with { Hidden = true });
    }

    [Fact]
    public void ADraftHoldsEachPictureItPlacesOnce()
    {
        MediaRef photo = new() { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg" };

        List<MediaRef> held = Rules.MediaOfDraft([TileWith(photo), TileWith(photo), TileWith(photo) with { Kind = PatchKind.Remove, Values = null }]);

        Assert.Equal([photo.Id], held.Select(media => media.Id));
    }

    private PlacementRules Rules => new(TestSurfaces.Catalog, TestSurfaces.Permissions, _library, TestSurfaces.Places);

    private bool May(GrantSet grants, string areaKey, string? scopeId, bool publishing)
    {
        (_, string? scope, Permission needed) = Rules.Required(areaKey, scopeId, publishing);
        return grants.IsAllowed(needed, scope);
    }

    private Task<PlacementState> ValidatedAsync(PlacementState state) =>
        Rules.ValidatedAsync(TestSurfaces.Catalog.ByKey[state.Surface], state, TestContext.Current.CancellationToken);

    private static GrantSet Holding(params (Permission Permission, string? ScopeId)[] held) =>
        new([.. held.Select(entry => new Grant(PermissionPattern.Parse(entry.Permission.Node),
            entry.ScopeId is null ? Scope.Global : Scope.Of(entry.Permission.ScopeKind, entry.ScopeId)))]);

    private static PlacementState State(Surface surface, params (string Key, string Value)[] values) =>
        new(surface.Key, 0, false, "", null, null, new(values.ToDictionary(value => value.Key, value => value.Value)));

    private static (string, string) Image(MediaRef media) => (HubSurfaces.LinkTile.Image.Key, HubSurfaces.LinkTile.Image.Format(media));

    private static PlacementPatch TileWith(MediaRef media) =>
        new(Guid.CreateVersion7().ToString(), HubSurfaces.Area.Key, null, HubSurfaces.LinkTile.Surface.Key, PatchKind.Add,
            Values: new Dictionary<string, string?> { [HubSurfaces.LinkTile.Image.Key] = HubSurfaces.LinkTile.Image.Format(media) });
}
