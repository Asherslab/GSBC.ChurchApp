using System.Globalization;
using System.Text;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public enum ThemeScheme
{
    Light,
    Dark,
}

public sealed record ThemeKeys(string Masthead, string Primary, string Accent, string Marker, string Tint);

public sealed record ThemeFont(string Key, string Name, string Family, int Weight, string Tracking)
{
    private const string Fallback = "-apple-system, system-ui, sans-serif";

    public CustomFont? Custom { get; private init; }

    public static ThemeFont Of(CustomFont custom) =>
        new(custom.CssFamily, custom.Family, $"\"{custom.CssFamily}\", {Fallback}", custom.HasBold ? 700 : 400, "0") { Custom = custom };
}

public sealed record ThemeRole(string Key, string Label);

public sealed record ThemeProblem(ThemeScheme Scheme, string TextRole, string BackgroundRole, double Contrast, string Words)
{
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Scheme}: {Words} would be hard to read ({Contrast:0.0}:1; it needs {ThemeDerivation.ReadableText}:1).");
}

public sealed record ThemeSpec(
    string Name, ThemeFont Font, ThemeFont BodyFont, ThemeKeys Light, ThemeKeys Dark,
    IReadOnlyDictionary<string, string> LightOverrides, IReadOnlyDictionary<string, string> DarkOverrides)
{
    public IReadOnlyDictionary<string, string> Roles(ThemeScheme scheme) =>
        scheme == ThemeScheme.Light ? ThemeDerivation.Light(Light, LightOverrides) : ThemeDerivation.Dark(Dark, DarkOverrides);
}

public readonly record struct Rgb(double R, double G, double B)
{
    public static bool TryParse(string text, out Rgb colour)
    {
        colour = default;
        if (text.Length != 7 || text[0] != '#' || !int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
            return false;
        colour = new Rgb((value >> 16) & 0xff, (value >> 8) & 0xff, value & 0xff);
        return true;
    }

    public static Rgb Parse(string text) => TryParse(text, out Rgb colour) ? colour : throw new FormatException($"'{text}' isn't a colour like #2a3d5e.");

    public Rgb Mix(Rgb other, double amountOfOther) =>
        new(R + (other.R - R) * amountOfOther, G + (other.G - G) * amountOfOther, B + (other.B - B) * amountOfOther);

    public double Luminance => 0.2126 * Linear(R) + 0.7152 * Linear(G) + 0.0722 * Linear(B);

    public double ContrastWith(Rgb other)
    {
        (double light, double dark) = Luminance > other.Luminance ? (Luminance, other.Luminance) : (other.Luminance, Luminance);
        return (light + 0.05) / (dark + 0.05);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"#{Byte(R):x2}{Byte(G):x2}{Byte(B):x2}");

    private static int Byte(double channel) => (int)Math.Round(Math.Clamp(channel, 0, 255));

    private static double Linear(double channel)
    {
        double value = channel / 255;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}

public static class ThemeDerivation
{
    public const double ReadableText = 4.5;

    public static readonly Rgb White = new(255, 255, 255);
    public static readonly Rgb Black = new(0, 0, 0);

    public static readonly IReadOnlyList<ThemeRole> Editable =
    [
        new("page", "Page"),
        new("surface", "Raised panels"),
        new("sand", "Shaded boxes and fields"),
        new("sand-strong", "Tracks and placeholders"),
        new("ink", "Text"),
        new("ink-2", "Secondary text"),
        new("ink-3", "Quiet text"),
        new("line", "Dividers"),
        new("masthead", "Masthead"),
        new("masthead-deep", "Masthead, deeper"),
        new("on-masthead", "Text on the masthead"),
        new("masthead-note", "Note line on the masthead"),
        new("primary", "Main buttons"),
        new("on-primary", "Text on main buttons"),
        new("accent", "Links and actions"),
        new("live", "Live and switches"),
        new("on-live", "Text on live"),
        new("marker", "Marker: here and new"),
        new("marker-ink", "Marker text"),
        new("on-marker", "Text on the marker"),
        new("danger", "Danger"),
    ];

    private static readonly (string Text, string Background, string Words)[] ReadablePairs =
    [
        ("ink", "page", "text"),
        ("ink-2", "page", "secondary text"),
        ("on-masthead", "masthead", "text on the masthead"),
        ("masthead-note", "masthead", "the note line on the masthead"),
        ("on-primary", "primary", "text on main buttons"),
        ("accent", "page", "links"),
        ("marker-ink", "page", "marker text"),
        ("on-marker", "marker", "text on the marker"),
    ];

    public static IReadOnlyDictionary<string, string> Light(ThemeKeys keys, IReadOnlyDictionary<string, string> overrides)
    {
        Rgb masthead = Rgb.Parse(keys.Masthead);
        Rgb primary = Rgb.Parse(keys.Primary);
        Rgb accent = Rgb.Parse(keys.Accent);
        Rgb marker = Rgb.Parse(keys.Marker);
        Rgb tint = Rgb.Parse(keys.Tint);
        Rgb page = White;
        Rgb ink = Readable(masthead.Mix(Black, 0.45), page);
        Rgb onMasthead = TextOn(masthead);

        Dictionary<string, string> roles = new()
        {
            ["page"] = page.ToString(),
            ["surface"] = page.ToString(),
            ["sand"] = tint.ToString(),
            ["sand-strong"] = tint.Mix(masthead, 0.08).ToString(),
            ["ink"] = ink.ToString(),
            ["ink-2"] = Readable(ink.Mix(White, 0.3), page).ToString(),
            ["ink-3"] = Readable(ink.Mix(White, 0.42), page).ToString(),
            ["line"] = page.Mix(masthead, 0.12).ToString(),
            ["masthead"] = masthead.ToString(),
            ["masthead-deep"] = masthead.Mix(Black, 0.15).ToString(),
            ["on-masthead"] = onMasthead.ToString(),
            ["masthead-note"] = Readable(onMasthead.Mix(marker, 0.3), masthead).ToString(),
            ["primary"] = primary.ToString(),
            ["on-primary"] = TextOn(primary).ToString(),
            ["accent"] = Readable(accent, page).ToString(),
            ["live"] = accent.ToString(),
            ["on-live"] = TextOn(accent).ToString(),
            ["marker"] = marker.ToString(),
            ["marker-ink"] = Readable(marker.Mix(Black, 0.4), page).ToString(),
            ["on-marker"] = TextOn(marker).ToString(),
            ["danger"] = "#b4372f",
        };
        return WithDetails(Apply(roles, overrides), ThemeScheme.Light);
    }

    public static IReadOnlyDictionary<string, string> Dark(ThemeKeys keys, IReadOnlyDictionary<string, string> overrides)
    {
        Rgb masthead = Rgb.Parse(keys.Masthead);
        Rgb primary = Rgb.Parse(keys.Primary);
        Rgb accent = Rgb.Parse(keys.Accent);
        Rgb marker = Rgb.Parse(keys.Marker);
        Rgb tint = Rgb.Parse(keys.Tint);
        Rgb page = DarkEnough(masthead.Mix(Black, 0.5));
        Rgb ink = Readable(White.Mix(marker, 0.06), page);
        Rgb onMasthead = TextOn(masthead);

        Dictionary<string, string> roles = new()
        {
            ["page"] = page.ToString(),
            ["surface"] = tint.ToString(),
            ["sand"] = tint.ToString(),
            ["sand-strong"] = tint.Mix(White, 0.06).ToString(),
            ["ink"] = ink.ToString(),
            ["ink-2"] = Readable(ink.Mix(page, 0.3), page).ToString(),
            ["ink-3"] = Readable(ink.Mix(page, 0.45), page).ToString(),
            ["line"] = page.Mix(White, 0.09).ToString(),
            ["masthead"] = masthead.ToString(),
            ["masthead-deep"] = masthead.Mix(Black, 0.2).ToString(),
            ["on-masthead"] = onMasthead.ToString(),
            ["masthead-note"] = Readable(onMasthead.Mix(marker, 0.3), masthead).ToString(),
            ["primary"] = primary.ToString(),
            ["on-primary"] = TextOn(primary).ToString(),
            ["accent"] = Readable(accent, page).ToString(),
            ["live"] = accent.ToString(),
            ["on-live"] = TextOn(accent).ToString(),
            ["marker"] = marker.ToString(),
            ["marker-ink"] = Readable(marker.Mix(White, 0.3), page).ToString(),
            ["on-marker"] = TextOn(marker).ToString(),
            ["danger"] = "#f08a80",
        };
        return WithDetails(Apply(roles, overrides), ThemeScheme.Dark);
    }

    public static ThemeKeys DarkKeysFrom(ThemeKeys light)
    {
        Rgb masthead = Rgb.Parse(light.Masthead);
        Rgb accent = Rgb.Parse(light.Accent);
        Rgb marker = Rgb.Parse(light.Marker);
        Rgb darkPage = DarkEnough(masthead.Mix(Black, 0.5));
        Rgb brightAccent = Readable(accent, darkPage);
        return new ThemeKeys(
            masthead.Mix(Black, 0.08).ToString(),
            brightAccent.ToString(),
            brightAccent.ToString(),
            marker.ToString(),
            darkPage.Mix(masthead, 0.35).ToString());
    }

    public static IReadOnlyList<string> Problems(ThemeSpec theme) => ProblemsOf(theme).Select(problem => problem.ToString()).ToList();

    public static IReadOnlyList<ThemeProblem> ProblemsOf(ThemeSpec theme) =>
        [.. ProblemsIn(theme.Roles(ThemeScheme.Light), ThemeScheme.Light), .. ProblemsIn(theme.Roles(ThemeScheme.Dark), ThemeScheme.Dark)];

    public static IReadOnlyList<MediaRef> FontFiles(ThemeSpec theme) =>
        new[] { theme.Font.Custom, theme.BodyFont.Custom }.OfType<CustomFont>().SelectMany(font => font.Files).Select(file => file.Media).DistinctBy(media => media.Key).ToList();

    public static string Css(ThemeSpec theme, string lightSelector, string darkSelector, IReadOnlyDictionary<string, string>? fontSources = null)
    {
        StringBuilder css = new();
        foreach (CustomFont font in new[] { theme.Font.Custom, theme.BodyFont.Custom }.OfType<CustomFont>().DistinctBy(font => font.CssFamily))
            FontFaces(css, font, fontSources ?? new Dictionary<string, string>());
        Block(css, lightSelector, theme.Roles(ThemeScheme.Light), theme);
        Block(css, darkSelector, theme.Roles(ThemeScheme.Dark), theme);
        return css.ToString();
    }

    private static void FontFaces(StringBuilder css, CustomFont font, IReadOnlyDictionary<string, string> sources)
    {
        foreach (FontFile file in font.Files)
        {
            if (!sources.TryGetValue(file.Media.Key, out string? source))
                continue;
            string safeSource = Uri.EscapeDataString(source).Replace("%3A", ":").Replace("%2F", "/");
            css.Append("@font-face { font-family: \"").Append(font.CssFamily).Append("\"; font-style: normal; font-weight: ")
                .Append(file.Weight is FontFile.Bold or FontFile.RegularToBold ? file.Weight : FontFile.Regular)
                .Append("; font-display: swap; src: url(\"").Append(safeSource).Append("\"); }\n");
        }
    }

    private static IEnumerable<ThemeProblem> ProblemsIn(IReadOnlyDictionary<string, string> roles, ThemeScheme scheme)
    {
        foreach ((string text, string background, string words) in ReadablePairs)
        {
            if (!Rgb.TryParse(roles[text], out Rgb foreground) || !Rgb.TryParse(roles[background], out Rgb behind))
                continue;
            double contrast = foreground.ContrastWith(behind);
            if (contrast < ReadableText)
                yield return new ThemeProblem(scheme, text, background, contrast, words);
        }
    }

    private static Dictionary<string, string> Apply(Dictionary<string, string> roles, IReadOnlyDictionary<string, string> overrides)
    {
        foreach ((string role, string value) in overrides)
        {
            if (roles.ContainsKey(role) && Rgb.TryParse(value, out _))
                roles[role] = value;
        }
        return roles;
    }

    private static Dictionary<string, string> WithDetails(Dictionary<string, string> roles, ThemeScheme scheme)
    {
        Rgb masthead = Rgb.Parse(roles["masthead"]);
        Rgb onMasthead = Rgb.Parse(roles["on-masthead"]);
        roles["on-masthead-2"] = onMasthead.Mix(masthead, 0.24).ToString();
        roles["masthead-chip"] = masthead.Mix(onMasthead, scheme == ThemeScheme.Light ? 0.12 : 0.1).ToString();
        roles["segment-on"] = scheme == ThemeScheme.Light ? roles["surface"] : Rgb.Parse(roles["surface"]).Mix(White, 0.14).ToString();
        roles["selected-fill"] = scheme == ThemeScheme.Light ? roles["sand"] : roles["segment-on"];
        roles["fill-on-surface"] = scheme == ThemeScheme.Light ? roles["sand"] : roles["sand-strong"];
        roles["knob"] = scheme == ThemeScheme.Light ? "#ffffff" : roles["ink"];
        roles["bar"] = $"color-mix(in srgb, {roles["page"]} 90%, transparent)";
        return roles;
    }

    private static void Block(StringBuilder css, string selector, IReadOnlyDictionary<string, string> roles, ThemeSpec theme)
    {
        ThemeFont font = theme.Font;
        css.Append(selector).Append(" {");
        foreach ((string role, string value) in roles)
            css.Append(" --").Append(role).Append(": ").Append(value).Append(';');
        css.Append(" --font: ").Append(theme.BodyFont.Family).Append(';');
        css.Append(" --font-masthead: ").Append(font.Family).Append(';');
        css.Append(" --masthead-weight: ").Append(font.Weight.ToString(CultureInfo.InvariantCulture)).Append(';');
        css.Append(" --masthead-tracking: ").Append(font.Tracking).Append("; }\n");
    }

    private static Rgb TextOn(Rgb background)
    {
        Rgb dark = background.Mix(Black, 0.85);
        if (White.ContrastWith(background) >= ReadableText)
            return White;
        return dark.ContrastWith(background) >= ReadableText ? dark : Black;
    }

    private static Rgb DarkEnough(Rgb page)
    {
        const double darkPage = 0.02;
        Rgb adjusted = page;
        for (int step = 1; step <= 20 && adjusted.Luminance > darkPage; step++)
            adjusted = page.Mix(Black, step * 0.05);
        return adjusted;
    }

    private static Rgb Readable(Rgb colour, Rgb background)
    {
        Rgb towards = Black.ContrastWith(background) > White.ContrastWith(background) ? Black : White;
        Rgb adjusted = colour;
        for (int step = 1; step <= 20 && adjusted.ContrastWith(background) < ReadableText; step++)
            adjusted = colour.Mix(towards, step * 0.05);
        return adjusted;
    }
}
