using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class AppearanceSurfaces
{
    public const string EditNode = "config.appearance.edit";
    public const string Brand = "brand";
    public const string Year2026 = "year-2026";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Choose the app's colours for everyone: the church's brand or this year's theme");

    public const string PublishNode = "config.appearance.publish";

    public static readonly Permission Publish =
        new(PublishNode, ScopeKind.Global, "Put a change of colours out to everyone, or on for an event");

    public static readonly SurfaceArea Area = new("app.appearance", "Colours", "The colours everyone sees in the app", ScopeKind.Global, Edit, Publish, Ordered: false);

    public static readonly IReadOnlyList<Choice> Palettes =
    [
        new(Brand, "The church's brand: navy and aqua"),
        new(Year2026, "2026 theme, Almighty God: greens"),
    ];

    public static readonly IReadOnlyList<ThemeFont> Fonts =
    [
        new("montserrat", "Montserrat", "\"Montserrat\", -apple-system, system-ui, sans-serif", 700, "-0.03em"),
        new("dm-serif-text", "DM Serif Text", "\"DM Serif Text\", Georgia, serif", 400, "-0.01em"),
    ];

    public static class Theme
    {
        public static readonly ThemeChoiceSetting Palette = new("palette", "Colours", Palettes, Brand,
            help: "Set dates to run a theme for a season; outside them, and when hidden, the app uses the church's brand.");

        public static readonly Surface Surface = new("app.theme", Area, "Theme", "Which colours the app uses", [Palette]);
    }

    public static class CustomTheme
    {
        public static readonly TextSetting Name = new("name", "Name", defaultValue: "New theme", required: true, maxLength: 40);
        public static readonly FontSetting Font = new("font", "Masthead font", Fonts, "montserrat");
        public static readonly FontSetting BodyFont = new("body-font", "Text font", Fonts, "montserrat",
            help: "Used for everything else. Keep it easy to read at small sizes.");

        public static readonly HexColourSetting LightMasthead = new("light-masthead", "Masthead", "#2a3d5e");
        public static readonly HexColourSetting LightPrimary = new("light-primary", "Main buttons", "#2a3d5e");
        public static readonly HexColourSetting LightAccent = new("light-accent", "Links and live", "#00877c");
        public static readonly HexColourSetting LightMarker = new("light-marker", "Marker", "#e7ae74");
        public static readonly HexColourSetting LightTint = new("light-tint", "Shaded boxes", "#f4efe7");

        public static readonly HexColourSetting DarkMasthead = new("dark-masthead", "Masthead", "", workedOutWhenEmpty: true);
        public static readonly HexColourSetting DarkPrimary = new("dark-primary", "Main buttons", "", workedOutWhenEmpty: true);
        public static readonly HexColourSetting DarkAccent = new("dark-accent", "Links and live", "", workedOutWhenEmpty: true);
        public static readonly HexColourSetting DarkMarker = new("dark-marker", "Marker", "", workedOutWhenEmpty: true);
        public static readonly HexColourSetting DarkTint = new("dark-tint", "Raised panels", "", workedOutWhenEmpty: true);

        public static readonly ColourOverridesSetting Overrides = new("overrides", "Advanced colours",
            help: "Set any colour by hand; everything else is worked out from the key colours.");

        public static readonly IReadOnlyList<HexColourSetting> LightKeys = [LightMasthead, LightPrimary, LightAccent, LightMarker, LightTint];
        public static readonly IReadOnlyList<HexColourSetting> DarkKeys = [DarkMasthead, DarkPrimary, DarkAccent, DarkMarker, DarkTint];

        public static readonly Surface Surface = new("app.custom-theme", Area, "Custom theme",
            "A theme you make: a name, a masthead font and its colours for light and dark",
            [Name, Font, BodyFont, .. LightKeys, .. DarkKeys, Overrides], defaultOrder: 100, repeatable: true,
            check: values => ThemeDerivation.Problems(SpecOf(values)));

        public static ThemeSpec SpecOf(SettingValues values)
        {
            IReadOnlyDictionary<string, string> overrides = values.Get(Overrides);
            return new ThemeSpec(
                values.Get(Name),
                values.Get(Font),
                values.Get(BodyFont),
                new ThemeKeys(values.Get(LightMasthead), values.Get(LightPrimary), values.Get(LightAccent), values.Get(LightMarker), values.Get(LightTint)),
                DarkKeysOf(values),
                OverridesFor(overrides, ThemeScheme.Light),
                OverridesFor(overrides, ThemeScheme.Dark));
        }

        public static ThemeKeys DarkKeysOf(SettingValues values)
        {
            ThemeKeys light = new(values.Get(LightMasthead), values.Get(LightPrimary), values.Get(LightAccent), values.Get(LightMarker), values.Get(LightTint));
            ThemeKeys worked = ThemeDerivation.DarkKeysFrom(light);
            string Either(HexColourSetting setting, string workedOut) => values.Get(setting) is { Length: > 0 } set ? set : workedOut;
            return new ThemeKeys(Either(DarkMasthead, worked.Masthead), Either(DarkPrimary, worked.Primary), Either(DarkAccent, worked.Accent),
                Either(DarkMarker, worked.Marker), Either(DarkTint, worked.Tint));
        }

        private static Dictionary<string, string> OverridesFor(IReadOnlyDictionary<string, string> overrides, ThemeScheme scheme)
        {
            string prefix = ColourOverridesSetting.RoleKey(scheme, "");
            return overrides.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key[prefix.Length..], pair => pair.Value);
        }
    }

    public static SurfaceSet All => new([Theme.Surface, CustomTheme.Surface]);

    public sealed record ChosenTheme(string Palette, ThemeSpec? Custom, string ChosenValue);

    public const string CustomPalette = "custom";
    public const string PreviewPalette = "preview-custom";

    public static ChosenTheme ChosenFrom(IEnumerable<ArrangedSurface> shown)
    {
        List<ArrangedSurface> entries = shown.ToList();
        string palette = entries.FirstOrDefault(entry => entry.Surface.Key == Theme.Surface.Key)?.Values.Get(Theme.Palette) ?? Brand;
        if (!ThemeChoiceSetting.IsCustom(palette, out string themeId))
        {
            string known = Palettes.Any(choice => choice.Value == palette) ? palette : Brand;
            return new ChosenTheme(known, null, known);
        }

        ArrangedSurface? custom = entries.FirstOrDefault(entry => entry.Surface.Key == CustomTheme.Surface.Key && entry.Placement?.Id == themeId);
        return custom is null ? new ChosenTheme(Brand, null, Brand) : new ChosenTheme(CustomPalette, CustomTheme.SpecOf(custom.Values), palette);
    }

    public static string PaletteOf(IEnumerable<ArrangedSurface> shown) => ChosenFrom(shown).Palette;
}
