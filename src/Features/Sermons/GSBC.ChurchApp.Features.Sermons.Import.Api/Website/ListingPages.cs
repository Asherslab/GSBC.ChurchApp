using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal sealed record ListedSermon(string Path, string Title);

internal sealed record ListingPage(IReadOnlyList<ListedSermon> Sermons, int LastPage, int? WebsiteCount);

internal static partial class ListingPages
{
    public static string PathOf(int page) => $"/podcasts/media?page={page}";

    public static ListingPage Read(string html)
    {
        List<ListedSermon> sermons = Entry().Matches(html)
            .Select(match => new ListedSermon(match.Groups["path"].Value, Html.Text(match.Groups["title"].Value)))
            .DistinctBy(sermon => sermon.Path)
            .ToList();
        int last = PageLink().Matches(html).Select(match => int.Parse(match.Groups["page"].Value)).DefaultIfEmpty(1).Max();
        MatchCollection years = YearBadge().Matches(html);
        return new ListingPage(sermons, last, years.Count == 0 ? null : years.Sum(year => int.Parse(year.Groups["count"].Value)));
    }

    [GeneratedRegex("""<a class="row d-sm-flex" href="(?<path>/media/[a-z0-9-]+)">.*?<h2[^>]*title="(?<title>[^"]*)">""", RegexOptions.Singleline)]
    private static partial Regex Entry();

    [GeneratedRegex("""href="#year\d{4}">\s*\d{4}\s*<span class="badge">(?<count>\d{1,5})</span>""")]
    private static partial Regex YearBadge();

    [GeneratedRegex(""""href="/podcasts/media\?page=(?<page>\d{1,4})[#"]"""")]
    private static partial Regex PageLink();
}
