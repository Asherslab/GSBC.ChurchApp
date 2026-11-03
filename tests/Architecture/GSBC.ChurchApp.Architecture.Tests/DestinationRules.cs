using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class DestinationRules
{
    [Fact]
    public void EveryPageDeclaredAsAPlaceIsARouteInTheApp()
    {
        HashSet<string> routes = Files("*.razor")
            .Where(path => path.Contains("/Hosts/App/") || AppProject().IsMatch(path))
            .SelectMany(path => PageRoute().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet();

        List<(string File, string Route)> declared = Files("*Destinations.cs")
            .SelectMany(path => DeclaredPage().Matches(File.ReadAllText(path)).Select(match => (Path.GetFileName(path), match.Groups[1].Value)))
            .ToList();
        List<string> missing = declared.Where(page => !routes.Contains(page.Route)).Select(page => $"{page.File}: {page.Route}").ToList();

        Assert.NotEmpty(declared);
        Assert.Empty(missing);
    }

    private static IEnumerable<string> Files(string pattern) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot.Path, "src"), pattern, SearchOption.AllDirectories)
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.Contains("/obj/") && !path.Contains("/bin/"));

    [GeneratedRegex(@"/Features/[^/]+/[^/]+\.App/")]
    private static partial Regex AppProject();

    [GeneratedRegex("@page \"([^\"{]*)\"")]
    private static partial Regex PageRoute();

    [GeneratedRegex("DestinationPage \\w+ = new\\(\"(/[^\"]*)\"")]
    private static partial Regex DeclaredPage();
}
