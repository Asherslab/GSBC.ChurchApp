using System.Net;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal sealed record TeamPhotos(IReadOnlyDictionary<string, string> ByName, IReadOnlyList<string> LeftOut);

internal static partial class TeamPage
{
    public const string Path = "/pages/our-team";

    public static TeamPhotos Read(string html)
    {
        List<(string Name, string Photo)> people = Person().Matches(html)
            .Select(match => (Name: WebUtility.HtmlDecode(match.Groups["name"].Value).Trim(), Photo: WebUtility.HtmlDecode(match.Groups["photo"].Value)))
            .Where(person => person.Name.Length > 0 && person.Photo.StartsWith("https://", StringComparison.Ordinal))
            .ToList();
        List<IGrouping<string, (string Name, string Photo)>> individuals = people.Where(person => !IsMoreThanOnePerson(person.Name))
            .GroupBy(person => Speakers.KeyOf(person.Name))
            .ToList();
        bool OnePhoto(IGrouping<string, (string Name, string Photo)> named) => named.Select(person => person.Photo).Distinct().Count() == 1;

        return new TeamPhotos(
            individuals.Where(OnePhoto).ToDictionary(named => named.First().Name, named => named.First().Photo, StringComparer.OrdinalIgnoreCase),
            [.. people.Where(person => IsMoreThanOnePerson(person.Name)).Select(person => person.Name).Distinct(), .. individuals.Where(named => !OnePhoto(named)).Select(named => named.First().Name)]);
    }

    private static bool IsMoreThanOnePerson(string name) =>
        name.Contains('&', StringComparison.Ordinal) || name.Contains(" and ", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("""<div id="user_\d+"[^>]*>\s*<div[^>]*>\s*<img\s[^>]*class="[^"]*rounded-circle[^"]*"[^>]*src="(?<photo>[^"]+)"[^>]*>.*?class="[^"]*\blead\b[^"]*">(?<name>[^<]*)<""", RegexOptions.Singleline)]
    private static partial Regex Person();
}
