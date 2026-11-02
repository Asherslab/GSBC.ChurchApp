using System.Globalization;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public abstract class Setting(string key, string label, string help)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string Help { get; } = help;

    public abstract string DefaultText { get; }

    public abstract string? ProblemWith(string text);
}

public abstract class Setting<T>(string key, string label, T defaultValue, string help) : Setting(key, label, help)
{
    public T Default { get; } = defaultValue;

    public override string DefaultText => Format(Default);

    public abstract bool TryParse(string text, out T value);

    public abstract string Format(T value);
}

public sealed class TextSetting(string key, string label, string defaultValue = "", bool required = false, int maxLength = 200, bool isLink = false, string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public bool Required { get; } = required;
    public int MaxLength { get; } = maxLength;
    public bool IsLink { get; } = isLink;

    public override bool TryParse(string text, out string value)
    {
        value = text;
        return ProblemWith(text) is null;
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text)
    {
        if (Required && string.IsNullOrWhiteSpace(text))
            return $"{Label} is needed.";
        if (text.Length > MaxLength)
            return $"{Label} can be up to {MaxLength} characters.";
        if (IsLink && text.Length > 0 && !IsWebLink(text))
            return $"{Label} must be a web link starting with https://.";
        return null;
    }

    private static bool IsWebLink(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps;
}

public sealed class NumberSetting(string key, string label, int defaultValue, int minimum, int maximum, string help = "")
    : Setting<int>(key, label, defaultValue, help)
{
    public int Minimum { get; } = minimum;
    public int Maximum { get; } = maximum;

    public override bool TryParse(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= Minimum && value <= Maximum;

    public override string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    public override string? ProblemWith(string text) =>
        TryParse(text, out _) ? null : $"{Label} must be a whole number from {Minimum} to {Maximum}.";
}

public sealed class YesNoSetting(string key, string label, bool defaultValue = false, string help = "")
    : Setting<bool>(key, label, defaultValue, help)
{
    public override bool TryParse(string text, out bool value)
    {
        value = text == Yes;
        return text is Yes or No;
    }

    public override string Format(bool value) => value ? Yes : No;

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} must be yes or no.";

    private const string Yes = "yes";
    private const string No = "no";
}

public sealed record Choice(string Value, string Label);

public class ChoiceSetting(string key, string label, IReadOnlyList<Choice> choices, string defaultValue, string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public IReadOnlyList<Choice> Choices { get; } = choices;

    public override bool TryParse(string text, out string value)
    {
        value = text;
        return Choices.Any(choice => choice.Value == text);
    }

    public override string Format(string value) => value;

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"'{text}' isn't one of the choices for {Label}.";
}

public sealed class ColourSetting(string key, string label, IReadOnlyList<Choice> tokens, string defaultToken, string help = "")
    : ChoiceSetting(key, label, tokens, defaultToken, help);

public sealed class DateSetting(string key, string label, DateOnly? defaultValue = null, string help = "")
    : Setting<DateOnly?>(key, label, defaultValue, help)
{
    private const string IsoDate = "yyyy-MM-dd";

    public override bool TryParse(string text, out DateOnly? value)
    {
        value = null;
        if (text.Length == 0)
            return true;
        if (!DateOnly.TryParseExact(text, IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            return false;
        value = date;
        return true;
    }

    public override string Format(DateOnly? value) => value?.ToString(IsoDate, CultureInfo.InvariantCulture) ?? "";

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} must be a date.";
}

public sealed class AudienceSetting(string key, string label, Audience? defaultValue = null, string help = "")
    : Setting<Audience>(key, label, defaultValue ?? Audience.Everyone, help)
{
    public override bool TryParse(string text, out Audience value)
    {
        bool parsed = Audience.TryParse(text, out Audience? audience);
        value = audience ?? Default;
        return parsed;
    }

    public override string Format(Audience value) => value.Text;

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} isn't an audience.";
}
