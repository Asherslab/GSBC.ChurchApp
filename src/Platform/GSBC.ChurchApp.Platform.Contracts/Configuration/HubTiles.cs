using System.Globalization;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public interface INormalises
{
    string Normalise(string text);
}

public sealed class LinkSetting(string key, string label, string defaultValue = "", string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public const int MostCharacters = 500;

    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text)
    {
        if (text.Length == 0)
            return null;
        if (text.Length > MostCharacters)
            return $"{Label} can be up to {MostCharacters} characters.";
        return LinkTargets.KindOf(text) switch
        {
            LinkKind.InApp => null,
            LinkKind.Web when text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) => null,
            _ => $"{Label} has to be a place in the app or a web page starting with https://.",
        };
    }
}

public sealed class IconSetting(string key, string label, string defaultValue = "", string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public const string CustomPrefix = "custom:";

    public static string Custom(string placementId) => CustomPrefix + placementId;

    public static bool IsCustom(string value, out string placementId)
    {
        placementId = value.StartsWith(CustomPrefix, StringComparison.Ordinal) ? value[CustomPrefix.Length..] : "";
        return Guid.TryParse(placementId, out _);
    }

    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text) =>
        text.Length == 0 || IsCustom(text, out _) || (text.Length <= 40 && text.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
            ? null
            : $"{Label} isn't an icon.";
}

public readonly record struct FocalPoint(int X, int Y)
{
    public static readonly FocalPoint Centre = new(50, 50);

    public string Css => $"{X}% {Y}%";
}

public sealed class FocusSetting(string key, string label, string help = "")
    : Setting<FocalPoint>(key, label, FocalPoint.Centre, help)
{
    public override bool TryParse(string text, out FocalPoint value)
    {
        value = FocalPoint.Centre;
        string[] parts = text.Split(' ');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
            || x is < 0 or > 100 || y is < 0 or > 100)
            return false;
        value = new FocalPoint(x, y);
        return true;
    }

    public override string Format(FocalPoint value) => string.Create(CultureInfo.InvariantCulture, $"{value.X} {value.Y}");

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} isn't a point on the picture.";
}

public sealed class SvgIconSetting(string key, string label, string help = "")
    : Setting<string>(key, label, "", help), INormalises
{
    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public string Normalise(string text) => SvgIcons.Sanitise(text) ?? text;

    public override string? ProblemWith(string text)
    {
        if (text.Length == 0)
            return $"{Label} is needed.";
        string? clean = SvgIcons.Sanitise(text);
        if (clean is null)
            return $"{Label} isn't an SVG drawing the app can show: it needs shapes and a size, up to {SvgIcons.MostCharactersKept / 1000} KB once cleaned.";
        return clean == text ? null : $"{Label} hasn't been cleaned. Upload it again.";
    }
}

public sealed record TileTone(string Value, string Label, string Background, string Text, bool Dark);

public static class TileTones
{
    public static readonly TileTone Sand = new("sand", "Shaded", "sand", "ink", false);
    public static readonly TileTone Raised = new("raised", "Raised", "surface", "ink", false);
    public static readonly TileTone Masthead = new("masthead", "Masthead", "masthead", "on-masthead", true);
    public static readonly TileTone Primary = new("primary", "Main buttons", "primary", "on-primary", true);
    public static readonly TileTone LiveSoft = new("live-soft", "Live, soft", "live", "ink", false);
    public static readonly TileTone Live = new("live", "Live", "live", "on-live", true);
    public static readonly TileTone MarkerSoft = new("marker-soft", "Marker, soft", "marker", "ink", false);
    public static readonly TileTone Marker = new("marker", "Marker", "marker", "on-marker", true);

    public static readonly IReadOnlyList<TileTone> All = [Sand, Raised, Masthead, Primary, LiveSoft, Live, MarkerSoft, Marker];

    public static TileTone Of(string value) => All.FirstOrDefault(tone => tone.Value == value) ?? Sand;

    public static IReadOnlyList<Choice> Choices => All.Select(tone => new Choice(tone.Value, tone.Label)).ToList();
}

public static class IconTones
{
    public const string Auto = "auto";
    public const string Own = "own";

    public static readonly IReadOnlyList<Choice> Choices =
    [
        new(Auto, "Goes with the tile"),
        new("primary", "Main colour"),
        new("accent", "Links and live"),
        new("warm", "Marker"),
        new("quiet", "Quiet"),
        new(Own, "Its own colours"),
    ];
}

public sealed record TileDefaults(
    string Title,
    string Text = "",
    int Columns = 1,
    int Rows = 1,
    TileTone? Tone = null,
    string Icon = "",
    string Link = "",
    string TitleLabel = "Title",
    string TextLabel = "Words under it",
    string TitleHelp = "",
    string TextHelp = "",
    bool TitleRequired = false);

public sealed record TileSettings(
    NumberSetting Columns,
    NumberSetting Rows,
    ColourSetting Tone,
    IconSetting Icon,
    ChoiceSetting IconTone,
    TextSetting Title,
    TextSetting Text,
    MediaSetting Image,
    FocusSetting Focus,
    LinkSetting Link)
{
    public IReadOnlyList<Setting> All => [Columns, Rows, Tone, Icon, IconTone, Title, Text, Image, Focus, Link];

    public static TileSettings For(TileDefaults defaults) => new(
        new NumberSetting(HubTileKeys.Columns, "Width", defaults.Columns, 1, HubGrid.MostColumns, help: "In columns of the grid"),
        new NumberSetting(HubTileKeys.Rows, "Height", defaults.Rows, 1, HubGrid.MostRows, help: "In rows of the grid"),
        new ColourSetting(HubTileKeys.Tone, "Colour", TileTones.Choices, (defaults.Tone ?? TileTones.Sand).Value),
        new IconSetting(HubTileKeys.Icon, "Icon", defaults.Icon),
        new ChoiceSetting(HubTileKeys.IconTone, "Icon colour", IconTones.Choices, IconTones.Auto),
        new TextSetting(HubTileKeys.Title, defaults.TitleLabel, defaults.Title, required: defaults.TitleRequired, maxLength: 60, help: defaults.TitleHelp),
        new TextSetting(HubTileKeys.Text, defaults.TextLabel, defaults.Text, maxLength: 140, help: defaults.TextHelp),
        new MediaSetting(HubTileKeys.Image, "Picture", help: "Fills the top of the tile, or all of a small one"),
        new FocusSetting(HubTileKeys.Focus, "Keep in view"),
        new LinkSetting(HubTileKeys.Link, "Opens", defaults.Link));
}

public static class HubTileKeys
{
    public const string Columns = "columns";
    public const string Rows = "rows";
    public const string Tone = "tone";
    public const string Icon = "icon";
    public const string IconTone = "icon-tone";
    public const string Title = "title";
    public const string Text = "text";
    public const string Image = "image";
    public const string Focus = "focus";
    public const string Link = "link";
}

public sealed class HubTileKind
{
    public HubTileKind(string key, string title, string description, string from, TileDefaults defaults, bool sample = false,
        IReadOnlyList<Setting>? more = null)
    {
        Tile = TileSettings.For(defaults);
        Defaults = defaults;
        From = from;
        Sample = sample;
        Surface = new Surface(key, HubSurfaces.Area, title, description, [.. Tile.All, .. more ?? []], defaultOrder: 1000, repeatable: true);
    }

    public Surface Surface { get; }
    public TileSettings Tile { get; }
    public TileDefaults Defaults { get; }
    public string From { get; }
    public bool Sample { get; }

    public bool IsPlain => From.Length == 0;
}

public sealed record TileLook(int Columns, int Rows, TileTone Tone, string Icon, string IconTone, string Title, string Text,
    Media.MediaRef? Image, FocalPoint Focus, string Link)
{
    public static TileLook Of(HubTileKind kind, SettingValues values) => new(
        values.Get(kind.Tile.Columns),
        values.Get(kind.Tile.Rows),
        TileTones.Of(values.Get(kind.Tile.Tone)),
        values.Get(kind.Tile.Icon),
        values.Get(kind.Tile.IconTone),
        values.Get(kind.Tile.Title),
        values.Get(kind.Tile.Text),
        values.Get(kind.Tile.Image),
        values.Get(kind.Tile.Focus),
        values.Get(kind.Tile.Link));
}
