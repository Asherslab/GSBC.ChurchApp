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
