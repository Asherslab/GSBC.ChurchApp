using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal sealed record CreditedTitle(string Title, string Speaker, string Honorific);

internal static partial class GuestTitles
{
    private static readonly Dictionary<string, string> Honorifics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bro"] = "Bro", ["Brother"] = "Brother", ["Pastor"] = "Pastor", ["Ps"] = "Ps", ["Evangelist"] = "Evangelist",
        ["Missionary"] = "Missionary", ["Missonary"] = "Missionary", ["Mrs"] = "Mrs", ["Mr"] = "Mr", ["Dr"] = "Dr",
        ["Sis"] = "Sis", ["Sister"] = "Sister", ["Rev"] = "Rev",
    };

    public static CreditedTitle? Split(string title)
    {
        string[] parts = title.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;
        if (Credit(parts[0]) is { } first)
            return new CreditedTitle(string.Join(" - ", parts[1..]), first.Name, first.Honorific);
        return Credit(parts[^1]) is { } last ? new CreditedTitle(string.Join(" - ", parts[..^1]), last.Name, last.Honorific) : null;
    }

    private static (string Honorific, string Name)? Credit(string part) =>
        Person().Match(part) is { Success: true } match && Honorifics.TryGetValue(match.Groups["honorific"].Value, out string? honorific)
            ? (honorific, match.Groups["name"].Value)
            : null;

    [GeneratedRegex(@"^(?<honorific>[A-Za-z]+)\.?\s+(?<name>[A-Z][\p{L}'’-]*(?:\s+(?:van|von|de|der|den|da|du|[A-Z][\p{L}'’-]*)){0,3})$")]
    private static partial Regex Person();
}
