using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("The church's brand and the 2026 theme are seeded once as ordinary themes, and the theme everyone sees can't be deleted")]
public sealed class ThemeSeedingTests : ConfigurationHost
{
    private static readonly (string, string?) ArrangesColours = (AppearanceSurfaces.EditNode, null);
    private static readonly (string, string?) PublishesColours = (AppearanceSurfaces.PublishNode, null);

    [Fact]
    public async Task BothThemesAreSeededOnceReadableInLightAndDarkAndTheBrandIsChosen()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesColours);
        Assert.True(await SeedAsync());
        List<Placement> seeded = await LiveAsync(staff, AppearanceSurfaces.Area);
        await PublishNowAsync(Access.Person(ArrangesColours, PublishesColours), AppearanceSurfaces.Area, null, Remove(Themes(seeded).Single(theme => theme.Id == AppearanceSurfaces.Seeded.Year2026Id)));

        Assert.False(await SeedAsync());
        Assert.Equal(["The church's brand", "2026 theme"], Themes(seeded).Select(theme => ValueOf(theme, AppearanceSurfaces.CustomTheme.Name.Key)));
        Assert.Equal(ThemeChoiceSetting.Custom(AppearanceSurfaces.Seeded.BrandId), ChosenIn(seeded));
        Assert.DoesNotContain(await LiveAsync(staff, AppearanceSurfaces.Area), placement => placement.Id == AppearanceSurfaces.Seeded.Year2026Id);
        foreach (ThemeSpec theme in Themes(seeded).Select(SpecOf))
        {
            Assert.Empty(ThemeDerivation.Problems(theme));
            Assert.True(Rgb.Parse(theme.Roles(ThemeScheme.Light)["page"]).Luminance > Rgb.Parse(theme.Roles(ThemeScheme.Dark)["page"]).Luminance);
        }
    }

    [Fact]
    public async Task AThemeChosenBeforeSeedingStaysChosen()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesColours, PublishesColours);
        await PublishNowAsync(staff, AppearanceSurfaces.Area, null, ThemeChoice("year-2026"));

        await SeedAsync();

        Assert.Equal(ThemeChoiceSetting.Custom(AppearanceSurfaces.Seeded.Year2026Id), ChosenIn(await LiveAsync(staff, AppearanceSurfaces.Area)));
    }

    [Fact]
    public async Task TheThemeEveryoneSeesCantBeDeleted()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesColours, PublishesColours);
        await SeedAsync();
        Placement brand = (await LiveAsync(staff, AppearanceSurfaces.Area)).Single(placement => placement.Id == AppearanceSurfaces.Seeded.BrandId);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => PublishNowAsync(staff, AppearanceSurfaces.Area, null, Remove(brand)));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.Contains("“The church's brand” is the theme everyone sees", refused.Status.Detail);
    }

    private Task<bool> SeedAsync() =>
        InScopeAsync(services => ThemeSeeding.SeedOnceAsync(services.GetRequiredService<ConfigurationDbContext>(), services.GetRequiredService<PlacementEditor>(), Cancel));

    private static IEnumerable<Placement> Themes(IEnumerable<Placement> appearance) =>
        appearance.Where(placement => placement.Surface == AppearanceSurfaces.CustomTheme.Surface.Key).OrderBy(placement => placement.Order);

    private static string ChosenIn(IEnumerable<Placement> appearance) =>
        new SettingValues(appearance.Single(placement => placement.Surface == AppearanceSurfaces.Theme.Surface.Key).Values.ToDictionary(value => value.Key, value => value.Value))
            .Get(AppearanceSurfaces.Theme.Palette);

    private static ThemeSpec SpecOf(Placement theme) =>
        AppearanceSurfaces.CustomTheme.SpecOf(new SettingValues(theme.Values.ToDictionary(value => value.Key, value => value.Value)));

    private static PatchMessage ThemeChoice(string palette) => new()
    {
        PlacementId = Guid.CreateVersion7().ToString(),
        Area = AppearanceSurfaces.Area.Key,
        Surface = AppearanceSurfaces.Theme.Surface.Key,
        Kind = (int)PatchKind.Add,
        Values = [new ValueChange { Key = AppearanceSurfaces.Theme.Palette.Key, Value = palette }],
    };
}
