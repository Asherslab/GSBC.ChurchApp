using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class AppearanceTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Surface[] Known = [AppearanceSurfaces.Theme.Surface];

    [Fact]
    public void TheBrandShowsUntilStaffChooseAnotherTheme() =>
        Assert.Equal(AppearanceSurfaces.Brand, PaletteWith());

    [Fact]
    public void StaffsChoiceShowsWhileItIsShownAndInItsDates()
    {
        Assert.Equal(AppearanceSurfaces.Year2026, PaletteWith(Theme(AppearanceSurfaces.Year2026)));
        Assert.Equal(AppearanceSurfaces.Brand, PaletteWith(Theme(AppearanceSurfaces.Year2026, hidden: true)));
        Assert.Equal(AppearanceSurfaces.Brand, PaletteWith(Theme(AppearanceSurfaces.Year2026, endsAt: Now)));
    }

    [Fact]
    public void APaletteThisAppDoesNotKnowFallsBackToTheBrand() =>
        Assert.Equal(AppearanceSurfaces.Brand, PaletteWith(Theme("year-2031")));

    private static string PaletteWith(params StoredPlacement[] stored) =>
        AppearanceSurfaces.PaletteOf(SurfaceLayout.ShownTo(Known, stored, Viewer.Anonymous, Now));

    private static StoredPlacement Theme(string palette, bool hidden = false, DateTime? endsAt = null) =>
        new("1", AppearanceSurfaces.Theme.Surface.Key, 0, hidden, "", null, endsAt,
            new Dictionary<string, string> { [AppearanceSurfaces.Theme.Palette.Key] = palette }, 1);
}
