using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class HostLinkRules
{
    private static readonly CalendarLine SampleCalendar = new() { Id = "0198c7a2-0000-7000-8000-000000000001", Slug = "kids" };

    [Fact]
    public void MinistryLinksInTheAppOpenAPageInTheApp()
    {
        string ministry = SampleCalendar.Id;
        string[] links =
        [
            MinistryLinks.App.Calendar(SampleCalendar), MinistriesPlaces.Directory, MinistriesPlaces.LifeGroups,
            .. new[] { MinistriesPlaces.About, MinistriesPlaces.News, MinistriesPlaces.Calendar, MinistriesPlaces.Files, MinistriesPlaces.Roster, MinistriesPlaces.Sundays, MinistriesPlaces.People, MinistriesPlaces.Settings }
                .Select(area => MinistriesPlaces.Of(ministry, area)),
        ];

        Assert.Empty(Unrouted(links, Routes(path => path.Contains("/Hosts/App/") || HostProject("App").IsMatch(path))));
    }

    [Fact]
    public void MinistryLinksInTheAdminPanelOpenAPageInTheAdminPanel() =>
        Assert.Empty(Unrouted([MinistryLinks.Admin.Calendar(SampleCalendar)], Routes(path => path.Contains("/Hosts/GSBC.ChurchApp.Hosts.Admin/") || HostProject("Admin").IsMatch(path))));

    private static List<string> Unrouted(IEnumerable<string> links, List<string[]> routes) =>
        links.Where(link => !routes.Any(route => Matches(route, Segments(link.Split('?', '#')[0])))).ToList();

    private static bool Matches(string[] route, string[] link) =>
        route.Length == link.Length && route.Zip(link).All(pair => pair.First.StartsWith('{') || string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase));

    private static List<string[]> Routes(Func<string, bool> inHost) =>
        RepoFiles.Under("src", ".razor")
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .Where(inHost)
            .SelectMany(path => PageRoute().Matches(File.ReadAllText(path)).Select(match => Segments(match.Groups[1].Value)))
            .ToList();

    private static string[] Segments(string path) => path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static Regex HostProject(string host) => new($@"/Features/[^/]+/[^/]+\.{host}/");

    [GeneratedRegex("@page \"([^\"]*)\"")]
    private static partial Regex PageRoute();
}
