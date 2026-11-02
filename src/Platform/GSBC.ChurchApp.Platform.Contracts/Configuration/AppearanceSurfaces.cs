using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class AppearanceSurfaces
{
    public const string EditNode = "config.appearance.edit";
    public const string Brand = "brand";
    public const string Year2026 = "year-2026";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Choose the app's colours for everyone: the church's brand or this year's theme");

    public static readonly SurfaceArea Area = new("app.appearance", "Colours", "The colours everyone sees in the app", ScopeKind.Global, Edit);

    public static readonly IReadOnlyList<Choice> Palettes =
    [
        new(Brand, "The church's brand: navy and aqua"),
        new(Year2026, "2026 theme, Almighty God: greens"),
    ];

    public static class Theme
    {
        public static readonly ChoiceSetting Palette = new("palette", "Colours", Palettes, Brand,
            help: "Set dates to run a theme for a season; outside them, and when hidden, the app uses the church's brand.");

        public static readonly Surface Surface = new("app.theme", Area, "Theme", "Which colours the app uses", [Palette]);
    }

    public static readonly SurfaceSet All = new([Theme.Surface]);

    public static string PaletteOf(IEnumerable<ArrangedSurface> shown) =>
        shown.FirstOrDefault(entry => entry.Surface.Key == Theme.Surface.Key)?.Values.Get(Theme.Palette) ?? Brand;
}
