using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class TestRules
{
    private const string SharedHelpers = "tests/Testing/";

    private static readonly (string Path, string Why)[] MayDependOnTheRun =
    [
        ("tests/Platform/GSBC.ChurchApp.Platform.Contracts.Tests/RichTextTests.cs",
            "It guards against quadratic parsing, which only time can show; the limit is far from both outcomes"),
        ("tests/Architecture/GSBC.ChurchApp.Architecture.Tests/TestRules.cs", "It names what the rule forbids"),
    ];

    [Fact]
    public void TestsRunTheSameEveryTime()
    {
        List<string> violations = TestFiles(".cs")
            .Where(path => MayDependOnTheRun.All(allowed => allowed.Path != path))
            .SelectMany(path => Lines(path).Where(line => DependsOnTheRun().IsMatch(line.Text)).Select(line => $"{line.Where}: {line.Text}"))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void OnlyTheSharedHelpersStartContainers()
    {
        List<string> violations = TestFiles(".cs").Concat(TestFiles(".csproj"))
            .SelectMany(path => Lines(path).Where(line => UsesTestcontainers().IsMatch(line.Text)).Select(line => $"{line.Where}: {line.Text}"))
            .ToList();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> TestFiles(string suffix) =>
        RepoFiles.Under("tests", suffix)
            .Select(path => Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.StartsWith(SharedHelpers, StringComparison.Ordinal));

    private static IEnumerable<(string Where, string Text)> Lines(string path) =>
        File.ReadLines(Path.Combine(RepoRoot.Path, path)).Select((text, index) => ($"{path}:{index + 1}", text.Trim()));

    [GeneratedRegex(@"\b(Task\.Delay|Thread\.Sleep|DateTime(Offset)?\.(Utc)?Now|TimeProvider\.System|Stopwatch|Random\.Shared)\b|new Random\(\)")]
    private static partial Regex DependsOnTheRun();

    [GeneratedRegex(@"using (DotNet\.)?Testcontainers|Include=""Testcontainers")]
    private static partial Regex UsesTestcontainers();
}
