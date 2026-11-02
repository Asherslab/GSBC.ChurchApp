using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public interface IHoldsMedia
{
    IReadOnlyList<MediaRef> MediaIn(string text);

    string WithMedia(string text, IReadOnlyDictionary<string, MediaRef> verified);
}

public sealed class MediaSetting(string key, string label, bool required = false, bool imagesOnly = true, string help = "")
    : Setting<MediaRef?>(key, label, null, help), IHoldsMedia
{
    public bool Required { get; } = required;
    public bool ImagesOnly { get; } = imagesOnly;

    public override bool TryParse(string text, out MediaRef? value)
    {
        value = null;
        return text.Length == 0 || ContentJson.TryRead(text, out value);
    }

    public override string Format(MediaRef? value) => value is null ? "" : ContentJson.Write(value);

    public override string? ProblemWith(string text)
    {
        if (!TryParse(text, out MediaRef? media))
            return $"{Label} isn't something from the media library.";
        if (media is null)
            return Required ? $"{Label} is needed." : null;
        if (ImagesOnly && !media.IsImage)
            return $"{Label} has to be an image.";
        if (media.IsPrivate)
            return $"Anyone can read the app's settings, so {Label} has to be a public image.";
        return null;
    }

    public IReadOnlyList<MediaRef> MediaIn(string text) => TryParse(text, out MediaRef? media) && media is not null ? [media] : [];

    public string WithMedia(string text, IReadOnlyDictionary<string, MediaRef> verified) =>
        TryParse(text, out MediaRef? media) && media is not null ? Format(verified[media.Id]) : text;
}

public sealed class RichTextSetting(string key, string label, bool required = false, int maxLength = 5_000, string help = "")
    : Setting<RichText>(key, label, new RichText(), help), IHoldsMedia
{
    public bool Required { get; } = required;
    public int MaxLength { get; } = maxLength;

    public override string DefaultText => "";

    public override bool TryParse(string text, out RichText value)
    {
        value = new RichText();
        if (text.Length == 0)
            return true;
        if (!ContentJson.TryRead(text, out RichText? content))
            return false;
        value = content!;
        return true;
    }

    public override string Format(RichText value) => value.IsEmpty && value.Media.Count == 0 ? "" : ContentJson.Write(value);

    public override string? ProblemWith(string text)
    {
        if (!TryParse(text, out RichText content))
            return $"{Label} isn't formatted text.";
        if (content.IsEmpty)
            return Required ? $"{Label} is needed." : null;
        IReadOnlyList<string> problems = RichTextRules.ProblemsWith(content, MaxLength);
        return problems.Count == 0 ? null : $"{Label}: {string.Join(" ", problems)}";
    }

    public IReadOnlyList<MediaRef> MediaIn(string text) =>
        TryParse(text, out RichText content) ? RichTextRules.WithOnlyUsedMedia(content).Media : [];

    public string WithMedia(string text, IReadOnlyDictionary<string, MediaRef> verified)
    {
        if (!TryParse(text, out RichText content))
            return text;
        RichText used = RichTextRules.WithOnlyUsedMedia(content);
        return Format(new RichText { Markdown = used.Markdown, Media = used.Media.Select(media => verified[media.Id]).ToList() });
    }
}

public sealed class FontSetting(string key, string label, IReadOnlyList<ThemeFont> bundled, string defaultKey, string help = "")
    : Setting<ThemeFont>(key, label, bundled.Single(font => font.Key == defaultKey), help), IHoldsMedia
{
    public IReadOnlyList<ThemeFont> Bundled { get; } = bundled;

    public override bool TryParse(string text, out ThemeFont value)
    {
        value = Default;
        if (text.StartsWith('{'))
        {
            if (!ContentJson.TryRead(text, out CustomFont? custom) || custom is null || custom.Files.Count == 0)
                return false;
            value = ThemeFont.Of(custom);
            return true;
        }
        ThemeFont? known = Bundled.FirstOrDefault(font => font.Key == text);
        value = known ?? Default;
        return known is not null;
    }

    public override string Format(ThemeFont value) => value.Custom is { } custom ? ContentJson.Write(custom) : value.Key;

    public override string? ProblemWith(string text)
    {
        if (!TryParse(text, out ThemeFont font))
            return $"{Label} isn't a font.";
        if (font.Custom is { } custom && custom.Files.Any(file => !file.Media.IsFont))
            return $"{Label} has to be a font file.";
        if (font.Custom is { } shared && shared.Files.Any(file => file.Media.IsPrivate))
            return $"Anyone can read the app's settings, so {Label} has to be a public font.";
        return null;
    }

    public IReadOnlyList<MediaRef> MediaIn(string text) =>
        TryParse(text, out ThemeFont font) && font.Custom is { } custom ? custom.Files.Select(file => file.Media).ToList() : [];

    public string WithMedia(string text, IReadOnlyDictionary<string, MediaRef> verified)
    {
        if (!TryParse(text, out ThemeFont font) || font.Custom is not { } custom)
            return text;
        foreach (FontFile file in custom.Files)
            file.Media = verified[file.Media.Id];
        return Format(font);
    }
}
