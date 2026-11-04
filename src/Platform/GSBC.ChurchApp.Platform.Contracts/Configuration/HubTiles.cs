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
            return $"This file can't be used as an icon. Try a simple drawing saved as SVG, under {SvgIcons.MostCharactersKept / 1000} KB.";
        return clean == text ? null : "Something went wrong with this icon. Upload it again.";
    }
}

public sealed record TileTone(string Value, string Label, string Background, string Text, bool Dark);

public static class TileTones
{
    public static readonly TileTone Sand = new("sand", "Soft", "sand", "ink", false);
    public static readonly TileTone Raised = new("raised", "Card", "surface", "ink", false);
    public static readonly TileTone Masthead = new("masthead", "Top of Home", "masthead", "on-masthead", true);
    public static readonly TileTone Primary = new("primary", "Buttons", "primary", "on-primary", true);
    public static readonly TileTone LiveSoft = new("live-soft", "Pale accent", "live", "ink", false);
    public static readonly TileTone Live = new("live", "Accent", "live", "on-live", true);
    public static readonly TileTone MarkerSoft = new("marker-soft", "Pale highlight", "marker", "ink", false);
    public static readonly TileTone Marker = new("marker", "Highlight", "marker", "on-marker", true);

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
        new("primary", "Buttons"),
        new("accent", "Accent"),
        new("warm", "Highlight"),
        new("quiet", "Grey"),
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
    bool TitleRequired = false,
    bool ShowsText = true);

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
    LinkSetting Link,
    ChoiceSetting Layout)
{
    public IReadOnlyList<Setting> All => [Columns, Rows, Tone, Icon, IconTone, Title, Text, Image, Focus, Link, Layout];

    public static TileSettings For(TileDefaults defaults, IReadOnlyList<TileVariant> variants) => new(
        new NumberSetting(HubTileKeys.Columns, "Width", defaults.Columns, 1, HubGrid.MostColumns, help: "How many columns across"),
        new NumberSetting(HubTileKeys.Rows, "Height", defaults.Rows, 1, HubGrid.MostRows, help: "How many rows down"),
        new ColourSetting(HubTileKeys.Tone, "Colour", TileTones.Choices, (defaults.Tone ?? TileTones.Sand).Value),
        new IconSetting(HubTileKeys.Icon, "Icon", defaults.Icon),
        new ChoiceSetting(HubTileKeys.IconTone, "Icon colour", IconTones.Choices, IconTones.Auto),
        new TextSetting(HubTileKeys.Title, defaults.TitleLabel, defaults.Title, required: defaults.TitleRequired, maxLength: 60, help: defaults.TitleHelp),
        new TextSetting(HubTileKeys.Text, defaults.TextLabel, defaults.Text, maxLength: 140, help: defaults.TextHelp),
        new MediaSetting(HubTileKeys.Image, "Picture", help: "Fills the top of the tile, or all of a small one"),
        new FocusSetting(HubTileKeys.Focus, "Keep in view"),
        new LinkSetting(HubTileKeys.Link, "Opens", defaults.Link),
        new ChoiceSetting(HubTileKeys.Layout, "Layout",
            [new Choice(TileVariant.Automatic, "Best for this size"), .. variants.Select(variant => new Choice(variant.Key, variant.Label))], TileVariant.Automatic));
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
    public const string Layout = "layout";
}

public sealed class HubTileKind
{
    public HubTileKind(string key, string title, string description, string from, TileDefaults defaults, bool sample = false,
        IReadOnlyList<Setting>? more = null, IReadOnlyList<TileVariant>? variants = null, bool offered = true)
    {
        Offered = offered;
        Variants = variants ?? TileVariants.Words;
        Tile = TileSettings.For(defaults, Variants);
        Defaults = defaults;
        From = from;
        Sample = sample;
        Surface = new Surface(key, HubSurfaces.Area, title, description, [.. Tile.All, .. more ?? []], defaultOrder: 1000, repeatable: true);
    }

    public Surface Surface { get; }
    public TileSettings Tile { get; }
    public IReadOnlyList<TileVariant> Variants { get; }
    public bool Offered { get; }

    public IReadOnlyList<TileVariant> VariantsFor(TileSpan span) => Variants.Where(variant => variant.Suits(span)).ToList();

    public TileVariant DefaultFor(TileSpan span, bool hasPicture) =>
        Variants.FirstOrDefault(variant => variant.Suits(span) && variant.IsDefault(span, hasPicture))
        ?? Variants.First(variant => variant.Suits(span));

    public TileVariant VariantFor(TileLook look, int gridColumns)
    {
        TileSpan span = new(Math.Clamp(look.Columns, 1, Math.Max(1, gridColumns)), look.Rows);
        TileVariant? chosen = Variants.FirstOrDefault(variant => variant.Key == look.Layout);
        return chosen is not null && chosen.SuitsTile(span, look.Image is not null) ? chosen : DefaultFor(span, look.Image is not null);
    }
    public TileDefaults Defaults { get; }
    public string From { get; }
    public bool Sample { get; }

    public bool IsPlain => From.Length == 0;
}

public sealed record TileLook(int Columns, int Rows, TileTone Tone, string Icon, string IconTone, string Title, string Text,
    Media.MediaRef? Image, FocalPoint Focus, string Link, string Layout = TileVariant.Automatic)
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
        values.Get(kind.Tile.Link),
        values.Get(kind.Tile.Layout));
}

public sealed record TileVariant(string Key, string Label, string Description, Func<TileSpan, bool> Suits, Func<TileSpan, bool, bool> IsDefault, bool NeedsPicture = false)
{
    public bool SuitsTile(TileSpan span, bool hasPicture) => Suits(span) && (hasPicture || !NeedsPicture);

    public const string Automatic = "auto";

    public static bool AnySize(TileSpan span) => true;

    public static bool Small(TileSpan span) => span is { Columns: 1, Rows: 1 };

    public static bool OneRowWide(TileSpan span) => span is { Rows: 1, Columns: > 1 };

    public static bool Tall(TileSpan span) => span.Rows > 1;

    public static bool Wide(TileSpan span) => span.Columns > 1;

    public static bool NotSmall(TileSpan span) => !Small(span);
}

public static class TileVariants
{
    public const string WordsKey = "words";
    public const string PictureAbove = "above";
    public const string PictureBehind = "behind";
    public const string PictureBeside = "beside";

    public static readonly IReadOnlyList<TileVariant> Words =
    [
        new(WordsKey, "Words and icon", "The title, the line under it and the icon; a picture isn't shown",
            TileVariant.AnySize, (_, picture) => !picture),
        new(PictureBehind, "Picture behind", "The picture fills the tile with the words over a shade",
            TileVariant.AnySize, (span, picture) => picture && TileVariant.Small(span), NeedsPicture: true),
        new(PictureAbove, "Picture above", "The picture fills the top and the words sit under it",
            TileVariant.Tall, (span, picture) => picture && span.Rows > 1, NeedsPicture: true),
        new(PictureBeside, "Picture beside", "A square picture at the start with the words beside it",
            TileVariant.Wide, (span, picture) => picture && TileVariant.OneRowWide(span), NeedsPicture: true),
    ];
}

public sealed class TemplateSetting(string key, string label, string help = "")
    : Setting<IReadOnlyDictionary<string, string>>(key, label, new Dictionary<string, string>(), help)
{
    public const int MostCharacters = 8_000;

    public override bool TryParse(string text, out IReadOnlyDictionary<string, string> value)
    {
        Dictionary<string, string> values = [];
        value = values;
        foreach (string pair in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length != 2)
                return false;
            values[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }
        return true;
    }

    public override string Format(IReadOnlyDictionary<string, string> value) =>
        string.Join('&', value.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

    public override string? ProblemWith(string text) =>
        text.Length > MostCharacters ? $"{Label} is too long to keep." : TryParse(text, out _) ? null : $"{Label} isn't a saved tile.";
}

public sealed class KindSetting(string key, string label) : Setting<string>(key, label, "", "")
{
    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text) =>
        text.Length is > 0 and <= 80 && text.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-') ? null : $"{Label} isn't a kind of tile.";
}
