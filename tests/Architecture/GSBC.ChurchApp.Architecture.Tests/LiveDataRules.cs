using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class LiveDataRules
{
    private static readonly string[] NotYetMigrated =
    [
    ];

    [Fact]
    public void ComponentsThatLoadDataRefreshThemselves()
    {
        List<string> violations = RepoFiles.Under("src", ".razor")
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => !NotYetMigrated.Contains(file.Path))
            .Where(file => InjectsData().IsMatch(file.Text) && LoadsOnStart().IsMatch(file.Text) && !Refreshes().IsMatch(file.Text))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(violations);
    }

    [GeneratedRegex(@"@inject (I?[A-Za-z]+Client|I[A-Za-z]+Reads|ILocalFeeds|IOutbox|IPeopleDirectory)\b")]
    private static partial Regex InjectsData();

    [GeneratedRegex(@"\bOn(Initialized|ParametersSet)(Async)?\b")]
    private static partial Regex LoadsOnStart();

    [GeneratedRegex(@"<LiveData\b|<RefreshOn\b|\+=")]
    private static partial Regex Refreshes();
}
