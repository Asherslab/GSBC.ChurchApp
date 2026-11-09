using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class AppearanceTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Surface[] Known = [AppearanceSurfaces.Theme.Surface, AppearanceSurfaces.CustomTheme.Surface];
    private static readonly string CustomId = Guid.CreateVersion7().ToString();

    [Fact]
    public void TheAppsOwnColoursShowUntilAThemeIsChosenAndWhenTheChosenThemeIsGone()
    {
        Assert.Equal(AppearanceSurfaces.Fallback, PaletteWith());
        Assert.Equal(AppearanceSurfaces.Fallback, PaletteWith(Theme(ThemeChoiceSetting.Custom(CustomId))));
    }

    [Fact]
    public void AChosenThemeShowsWhileItExistsIsShownAndIsInItsDates()
    {
        AppearanceSurfaces.ChosenTheme chosen = ChosenWith(Theme(ThemeChoiceSetting.Custom(CustomId)), Custom("Advent"));
        Assert.Equal(AppearanceSurfaces.CustomPalette, chosen.Palette);
        Assert.Equal("Advent", chosen.Custom?.Name);

        Assert.Equal(AppearanceSurfaces.Fallback, PaletteWith(Theme(ThemeChoiceSetting.Custom(CustomId), hidden: true), Custom("Advent")));
        Assert.Equal(AppearanceSurfaces.Fallback, PaletteWith(Theme(ThemeChoiceSetting.Custom(CustomId), endsAt: Now), Custom("Advent")));
    }

    [Theory]
    [InlineData("brand", AppearanceSurfaces.Seeded.BrandId)]
    [InlineData("year-2026", AppearanceSurfaces.Seeded.Year2026Id)]
    public void AChoiceMadeBeforeThemesWereSeededMeansTheSeededTheme(string former, string seeded) =>
        Assert.Equal(ThemeChoiceSetting.Custom(seeded), new SettingValues(new Dictionary<string, string> { [AppearanceSurfaces.Theme.Palette.Key] = former })
            .Get(AppearanceSurfaces.Theme.Palette));

    [Fact]
    public void AChoiceThisAppDoesNotKnowIsRefused() =>
        Assert.NotNull(AppearanceSurfaces.Theme.Palette.ProblemWith("year-2031"));

    [Theory]
    [InlineData("#ffffff", "#ffffff", "#ffffff", "#ffffff", "#ffffff")]
    [InlineData("#000000", "#000000", "#000000", "#000000", "#000000")]
    [InlineData("#ffd400", "#9be15d", "#00e5ff", "#ff7ab6", "#fff6c2")]
    [InlineData("#2a3d5e", "#2a3d5e", "#00877c", "#e7ae74", "#f4efe7")]
    [InlineData("#777777", "#767676", "#787878", "#757575", "#797979")]
    public void ThemesWorkedOutFromKeyColoursAreAlwaysReadable(string masthead, string primary, string accent, string marker, string tint)
    {
        Dictionary<string, string> values = KeyColours(masthead, primary, accent, marker, tint);
        Assert.Empty(AppearanceSurfaces.CustomTheme.Surface.ProblemsWith(values));
    }

    [Fact]
    public void AColourSetByHandThatCannotBeReadIsRefused()
    {
        Dictionary<string, string> values = KeyColours("#2a3d5e", "#2a3d5e", "#00877c", "#e7ae74", "#f4efe7");
        values[AppearanceSurfaces.CustomTheme.Overrides.Key] = $"{ColourOverridesSetting.RoleKey(ThemeScheme.Dark, "ink")}=#20314f";

        string problem = Assert.Single(AppearanceSurfaces.CustomTheme.Surface.ProblemsWith(values));
        Assert.StartsWith("Dark: text would be hard to read", problem);
    }

    [Fact]
    public void AFontsNameAsStaffTypedItNeverReachesTheStylesheet()
    {
        CustomFont font = new()
        {
            Family = "Evil\"; } body { display: none",
            Files = [new FontFile { Media = new MediaRef { Key = "ab/abcdef0123456789.woff2", ContentType = "font/woff2" }, Weight = FontFile.RegularToBold }],
        };
        Dictionary<string, string> values = KeyColours("#2a3d5e", "#2a3d5e", "#00877c", "#e7ae74", "#f4efe7");
        values[AppearanceSurfaces.CustomTheme.Font.Key] = AppearanceSurfaces.CustomTheme.Font.Format(ThemeFont.Of(font));
        ThemeSpec theme = AppearanceSurfaces.CustomTheme.SpecOf(new SettingValues(values));

        string css = ThemeDerivation.Css(theme, ":root", ":root.dark", new Dictionary<string, string> { [font.Files[0].Media.Key] = "https://media.test/a\"b.woff2" });

        Assert.DoesNotContain("Evil", css);
        Assert.DoesNotContain("a\"b", css);
        Assert.Contains("font-family: \"gsbc-font-abcdef0123456789\"", css);
    }

    private static Dictionary<string, string> KeyColours(string masthead, string primary, string accent, string marker, string tint)
    {
        Dictionary<string, string> values = new(AppearanceSurfaces.CustomTheme.Surface.Defaults.Raw) { [AppearanceSurfaces.CustomTheme.Name.Key] = "Test" };
        string[] colours = [masthead, primary, accent, marker, tint];
        foreach ((HexColourSetting key, string colour) in AppearanceSurfaces.CustomTheme.LightKeys.Zip(colours).Concat(AppearanceSurfaces.CustomTheme.DarkKeys.Zip(colours)))
            values[key.Key] = colour;
        return values;
    }

    private static AppearanceSurfaces.ChosenTheme ChosenWith(params StoredPlacement[] stored) =>
        AppearanceSurfaces.ChosenFrom(SurfaceLayout.ShownTo(Known, stored, Viewer.Anonymous, Now));

    private static StoredPlacement Custom(string name) =>
        new(CustomId, AppearanceSurfaces.CustomTheme.Surface.Key, 100, false, "", null, null,
            new Dictionary<string, string> { [AppearanceSurfaces.CustomTheme.Name.Key] = name }, 1);

    private static string PaletteWith(params StoredPlacement[] stored) =>
        AppearanceSurfaces.PaletteOf(SurfaceLayout.ShownTo(Known, stored, Viewer.Anonymous, Now));

    private static StoredPlacement Theme(string palette, bool hidden = false, DateTime? endsAt = null) =>
        new("1", AppearanceSurfaces.Theme.Surface.Key, 0, hidden, "", null, endsAt,
            new Dictionary<string, string> { [AppearanceSurfaces.Theme.Palette.Key] = palette }, 1);
}
