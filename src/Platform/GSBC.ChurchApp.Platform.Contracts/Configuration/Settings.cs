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

public sealed class HexColourSetting(string key, string label, string defaultValue, string help = "")
    : Setting<string>(key, label, defaultValue, help)
{
    public override bool TryParse(string text, out string value)
    {
        value = text.ToLowerInvariant();
        return Rgb.TryParse(value, out _);
    }

    public override string Format(string value) => value.ToLowerInvariant();

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} must be a colour like #2a3d5e.";
}

public sealed class ColourOverridesSetting(string key, string label, string help = "")
    : Setting<IReadOnlyDictionary<string, string>>(key, label, new Dictionary<string, string>(), help)
{
    private const char Separator = ';';
    private const char Assign = '=';

    public static string RoleKey(ThemeScheme scheme, string role) => $"{scheme.ToString().ToLowerInvariant()}.{role}";

    public override bool TryParse(string text, out IReadOnlyDictionary<string, string> value)
    {
        Dictionary<string, string> overrides = [];
        value = overrides;
        foreach (string pair in text.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = pair.Split(Assign, 2);
            if (parts.Length != 2 || !IsRole(parts[0]) || !Rgb.TryParse(parts[1], out _))
                return false;
            overrides[parts[0]] = parts[1].ToLowerInvariant();
        }
        return true;
    }

    public override string Format(IReadOnlyDictionary<string, string> value) =>
        string.Join(Separator, value.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}{Assign}{pair.Value}"));

    public override string? ProblemWith(string text) => TryParse(text, out _) ? null : $"{Label} has a colour that isn't a theme colour like #2a3d5e.";

    private static bool IsRole(string key) =>
        Enum.GetValues<ThemeScheme>().Any(scheme => ThemeDerivation.Editable.Any(role => key == RoleKey(scheme, role.Key)));
}

public sealed class ThemeChoiceSetting(string key, string label, IReadOnlyList<Choice> builtIn, string defaultValue, string help = "")
    : ChoiceSetting(key, label, builtIn, defaultValue, help)
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
        return Choices.Any(choice => choice.Value == text) || IsCustom(text, out _);
    }
}
