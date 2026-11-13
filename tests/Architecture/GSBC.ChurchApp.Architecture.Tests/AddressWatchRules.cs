using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class AddressWatchRules
{
    private static readonly Dictionary<string, string> Exempt = new()
    {
        ["src/Hosts/App/GSBC.ChurchApp.Hosts.App/Pages/OpenPlace.razor"] = "a link opens it once and it moves on; nothing moves between /open/… and /…",
    };

    [Fact]
    public void PagesThatReadTheirAddressWatchItMoveBetweenTheirRoutes()
    {
        List<string> violations = RepoFiles.Under("src", ".razor")
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => !Exempt.ContainsKey(file.Path))
            .Where(file => ReadsAddress().IsMatch(file.Text) && HasRoutesWithTheSameParameters(file.Text))
            .Where(file => !file.Text.Contains("<AddressWatch ", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(violations);
    }

    private static bool HasRoutesWithTheSameParameters(string text)
    {
        List<string> parameterSets = Route().Matches(text)
            .Select(route => string.Join(',', RouteParameter().Matches(route.Groups[1].Value).Select(parameter => parameter.Groups[1].Value).Order()))
            .ToList();
        return parameterSets.Count != parameterSets.Distinct().Count();
    }

    [GeneratedRegex(@"^@page ""([^""]*)""", RegexOptions.Multiline)]
    private static partial Regex Route();

    [GeneratedRegex(@"\{\*?([A-Za-z]+)")]
    private static partial Regex RouteParameter();

    [GeneratedRegex(@"\bNavigation\.(Uri|ToBaseRelativePath)\b")]
    private static partial Regex ReadsAddress();
}
