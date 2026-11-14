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

    [Fact]
    public void PagesWhoseDataDependsOnTheirAddressLoadAgainWhenOnlyItsValuesChange()
    {
        List<string> violations = RepoFiles.Under("src", ".razor")
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => !file.Text.Contains("<AddressWatch ", StringComparison.Ordinal) && !ReloadsOnNewParameters().IsMatch(file.Text))
            .SelectMany(file => UnkeyedLoadsReadingTheAddress(file.Text).Select(load => $"{file.Path}: {load}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> UnkeyedLoadsReadingTheAddress(string text)
    {
        List<string> parameters = Route().Matches(text)
            .SelectMany(route => RouteParameter().Matches(route.Groups[1].Value).Select(parameter => parameter.Groups[1].Value))
            .Distinct()
            .ToList();

        return LiveData().Matches(text)
            .Where(tag => !tag.Value.Contains("@key=", StringComparison.Ordinal))
            .Select(tag => LoadAttribute().Match(tag.Value))
            .Where(load => load.Success && parameters.Any(parameter => Regex.IsMatch(LoaderText(text, load.Groups[1].Value), $@"\b{parameter}\b")))
            .Select(load => load.Groups[1].Value);
    }

    private static string LoaderText(string text, string load) =>
        load.Contains("=>", StringComparison.Ordinal) ? load
        : Regex.Match(text, $@"\b{Regex.Escape(load)}\s*\([^)]*\)\s*(=>[^;]*;|\{{.*?\n    \}})", RegexOptions.Singleline).Value;

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

    [GeneratedRegex(@"<(LiveData|RefreshOn)\b(?:[^>]|(?<==)>)*>")]
    private static partial Regex LiveData();

    [GeneratedRegex(@"\bLoad=""([^""]*)""")]
    private static partial Regex LoadAttribute();

    [GeneratedRegex(@"OnParametersSet(Async)?\(\)(\s*=>[^;]*|\s*\{(?:(?!\n    \}).)*)Reload", RegexOptions.Singleline)]
    private static partial Regex ReloadsOnNewParameters();

    [GeneratedRegex(@"\bNavigation\.(Uri|ToBaseRelativePath)\b")]
    private static partial Regex ReadsAddress();
}
