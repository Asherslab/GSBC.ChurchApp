using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;

internal sealed record AddressQuery(string? Unit, string? Number, IReadOnlyList<string> Words)
{
    public bool IsEmpty => Number is null && Words.Count == 0;
}

internal static partial class AddressSearchText
{
    private const int MostPatterns = 8;

    private static readonly IReadOnlyDictionary<string, string> StreetTypes = new Dictionary<string, string>
    {
        ["st"] = "street", ["rd"] = "road", ["ave"] = "avenue", ["av"] = "avenue", ["ct"] = "court", ["crt"] = "court",
        ["dr"] = "drive", ["dve"] = "drive", ["pl"] = "place", ["cres"] = "crescent", ["cr"] = "crescent", ["crs"] = "crescent",
        ["tce"] = "terrace", ["cl"] = "close", ["pde"] = "parade", ["hwy"] = "highway", ["ln"] = "lane", ["cct"] = "circuit",
        ["cir"] = "circuit", ["bvd"] = "boulevard", ["blvd"] = "boulevard", ["esp"] = "esplanade", ["gr"] = "grove",
        ["gve"] = "grove", ["pkwy"] = "parkway", ["sq"] = "square", ["wy"] = "way", ["cnr"] = "corner", ["mt"] = "mount",
        ["ch"] = "chase", ["rdg"] = "ridge", ["rise"] = "rise", ["pwy"] = "pathway", ["sth"] = "south", ["nth"] = "north",
    };

    [GeneratedRegex(@"^(?:(?:unit|u|apt|apartment|flat|shop|suite|villa|townhouse|th)\s*)?(?<unit>\d+[a-z]?)\s*/\s*(?<number>\d+[a-z]?(?:-\d+[a-z]?)?)\b\s*(?<rest>.*)$")]
    private static partial Regex WithUnit();

    [GeneratedRegex(@"^(?<number>\d+[a-z]?(?:-\d+[a-z]?)?)\b\s*(?<rest>.*)$")]
    private static partial Regex WithNumber();

    public static string Normal(string text)
    {
        StringBuilder normal = new(text.Length);
        foreach (char letter in text.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(letter) == UnicodeCategory.NonSpacingMark || letter is '\'' or '’' or '.')
                continue;
            normal.Append(char.IsAsciiLetterOrDigit(letter) || letter is '/' or '-' ? letter : ' ');
        }
        return string.Join(' ', normal.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string Key(string street, string locality) =>
        string.Join(' ', Normal($"{street} {locality}").Split(' ').Where(word => word != "-"));

    public static AddressQuery Parse(string text)
    {
        string normal = Normal(text).Replace(" / ", "/").Replace("/ ", "/").Replace(" /", "/");
        if (WithUnit().Match(normal) is { Success: true } unit)
            return new AddressQuery(unit.Groups["unit"].Value, unit.Groups["number"].Value, Words(unit.Groups["rest"].Value));
        if (WithNumber().Match(normal) is { Success: true } number)
            return new AddressQuery(null, number.Groups["number"].Value, Words(number.Groups["rest"].Value));
        return new AddressQuery(null, null, Words(normal));
    }

    public static IReadOnlyList<string> Patterns(AddressQuery query)
    {
        IEnumerable<string> prefixes = [""];
        foreach (string word in query.Words)
            prefixes = prefixes.SelectMany(prefix => Spellings(word).Select(spelling => prefix.Length == 0 ? spelling : $"{prefix} {spelling}")).Take(MostPatterns);
        return [.. prefixes.Where(prefix => prefix.Length > 0).Select(prefix => $"{prefix}%")];
    }

    public static IReadOnlyList<string> BroadPatterns(AddressQuery query) => query.Words switch
    {
        [] => [],
        [var only] => [.. Spellings(only).Select(spelling => $"{spelling}%")],
        [var first, ..] => [.. Spellings(first).Select(spelling => $"{spelling} %")],
    };

    public static bool Matches(AddressQuery query, IAddressRow address)
    {
        if (query.Number is { } number && !NumberMatches(number, address.Number))
            return false;
        if (query.Unit is { } unit && address.Unit != unit)
            return false;
        string[] words = address.Key.Split(' ');
        int from = 0;
        foreach (string wanted in query.Words)
        {
            int at = Array.FindIndex(words, from, word => WordMatches(wanted, word));
            if (at < 0)
                return false;
            from = at + 1;
        }
        return true;
    }

    public static (int, int, int, int, string) Rank(AddressQuery query, IAddressRow address) =>
        (address.Number == query.Number ? 0 : 1,
            query.Unit is null && address.Unit.Length > 0 ? 1 : 0,
            Whole(address.Number),
            Whole(address.Unit),
            address.Key);

    public static bool NumberMatches(string wanted, string number) =>
        number == wanted || number.StartsWith($"{wanted}-", StringComparison.Ordinal);

    public static string Label(IAddressRow address)
    {
        string number = address.Unit.Length > 0 ? $"{address.Unit}/{address.Number}" : address.Number;
        string street = number.Length > 0 ? $"{number} {address.Street}" : address.Street;
        return address.Locality.Length > 0 ? $"{street}, {address.Locality}" : street;
    }

    private static IEnumerable<string> Spellings(string word) =>
        StreetTypes.TryGetValue(word, out string? full) ? [word, full] : [word];

    private static bool WordMatches(string wanted, string word) =>
        word.StartsWith(wanted, StringComparison.Ordinal) || (StreetTypes.TryGetValue(wanted, out string? full) && word == full);

    private static readonly HashSet<string> States = ["qld", "nsw", "vic", "tas", "sa", "wa", "nt", "act"];

    private static List<string> Words(string text) =>
        [.. text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(word => word != "-" && !States.Contains(word) && !IsPostcode(word))];

    private static bool IsPostcode(string word) => word.Length == 4 && word.All(char.IsAsciiDigit);

    private static int Whole(string number)
    {
        int digits = 0;
        while (digits < number.Length && char.IsAsciiDigit(number[digits]))
            digits++;
        return int.TryParse(number.AsSpan(0, digits), NumberStyles.None, CultureInfo.InvariantCulture, out int whole) ? whole : int.MaxValue;
    }
}
