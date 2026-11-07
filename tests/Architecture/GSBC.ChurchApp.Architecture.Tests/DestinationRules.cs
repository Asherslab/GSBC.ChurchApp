using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class DestinationRules
{
    [Fact]
    public void EveryPageDeclaredAsAPlaceIsARouteInTheApp()
    {
        HashSet<string> routes = Files(".razor")
            .Where(path => path.Contains("/Hosts/App/") || AppProject().IsMatch(path))
            .SelectMany(path => PageRoute().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet();

        List<(string File, string Route)> declared = Files("Destinations.cs")
            .SelectMany(path => DeclaredPage().Matches(File.ReadAllText(path)).Select(match => (Path.GetFileName(path), match.Groups[1].Value)))
            .ToList();
        List<string> missing = declared.Where(page => !routes.Contains(page.Route)).Select(page => $"{page.File}: {page.Route}").ToList();

        Assert.NotEmpty(declared);
        Assert.Empty(missing);
    }

    [Fact]
    public void NoTwoPagesInTheAppClaimTheSameRoute()
    {
        List<string> clashes = Files(".razor")
            .Where(path => path.Contains("/Hosts/App/") || AppProject().IsMatch(path))
            .SelectMany(path => AnyPageRoute().Matches(File.ReadAllText(path)).Select(match => (Path.GetFileName(path), Route: match.Groups[1].Value.ToLowerInvariant())))
            .GroupBy(page => page.Route)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(page => page.Item1))}")
            .ToList();

        Assert.Empty(clashes);
    }

    private static IEnumerable<string> Files(string suffix) =>
        RepoFiles.Under("src", suffix).Select(path => path.Replace(Path.DirectorySeparatorChar, '/'));

    [GeneratedRegex(@"/Features/[^/]+/[^/]+\.App/")]
    private static partial Regex AppProject();

    [GeneratedRegex("@page \"([^\"{]*)\"")]
    private static partial Regex PageRoute();

    [GeneratedRegex("@page \"([^\"]*)\"")]
    private static partial Regex AnyPageRoute();

    [GeneratedRegex("DestinationPage \\w+ = new\\(\"(/[^\"]*)\"")]
    private static partial Regex DeclaredPage();
}
