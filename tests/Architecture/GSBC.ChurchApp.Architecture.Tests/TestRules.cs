using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class TestRules
{
    private const string SharedHelpers = "tests/Testing/";
    private const string Rules = "tests/Architecture/";

    private static readonly (string Path, string Why)[] MayDependOnTheRun =
    [
        ("tests/Platform/GSBC.ChurchApp.Platform.Contracts.Tests/RichTextTests.cs",
            "It guards against quadratic parsing, which only time can show; the limit is far from both outcomes"),
        ("tests/Architecture/GSBC.ChurchApp.Architecture.Tests/TestRules.cs", "It names what the rule forbids"),
    ];

    private static readonly HashSet<string> NotYetSorted =
    [
        "tests/Features/Analytics/GSBC.ChurchApp.Features.Analytics.Api.Tests/DiagnosticsInboxTests.cs",
        "tests/Features/Analytics/GSBC.ChurchApp.Features.Analytics.Api.Tests/UsageTests.cs",
        "tests/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Api.Tests/DraftEditorTests.cs",
        "tests/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Api.Tests/HubTileTests.cs",
        "tests/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Api.Tests/PlacementEditorTests.cs",
        "tests/Features/Media/GSBC.ChurchApp.Features.Media.Api.Tests/MediaPrivacyAndFolderTests.cs",
        "tests/Features/Media/GSBC.ChurchApp.Features.Media.Api.Tests/MediaUploadTests.cs",
        "tests/Features/Media/GSBC.ChurchApp.Features.Media.Api.Tests/PictureSizeTests.cs",
        "tests/Features/Notifications/GSBC.ChurchApp.Features.Notifications.Api.Tests/NotificationsTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/ArtworkTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/AudioStorageTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/FollowingTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/LastingIdTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/ProgressTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/SourceRuleTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/UploadTests.cs",
        "tests/Features/Sermons/GSBC.ChurchApp.Features.Sermons.Api.Tests/WebsiteReadingTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/EmailDispatcherTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/FeedRetentionTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/HealthProbeTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/InstallProofTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/LiveFeedTests.cs",
        "tests/Platform/GSBC.ChurchApp.Platform.Api.Tests/SyncOriginTests.cs",
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
    public void ATestThatTouchesStorageSaysWhichStorageRuleItCovers()
    {
        List<string> violations = [];
        foreach (IGrouping<string, string> project in TestFiles(".cs").Where(path => !path.StartsWith(Rules, StringComparison.Ordinal)).GroupBy(ProjectOf))
        {
            Dictionary<string, string> texts = project.ToDictionary(path => path, path => File.ReadAllText(Path.Combine(RepoRoot.Path, path)));
            HashSet<string> reachesStorage = ReachesStorage(texts);
            foreach ((string path, string text) in texts.Where(file => TestMethod().IsMatch(file.Value) && !NotYetSorted.Contains(file.Key)))
            {
                bool saysWhy = IntegrationReason().Match(text) is { Success: true } reason && reason.Groups[1].Value.Trim().Length > 0;
                if (reachesStorage.Contains(path) && !saysWhy)
                    violations.Add($"{path}: reaches a database or S3 without [Integration(\"the storage rule it covers\")]");
                if (!reachesStorage.Contains(path) && IntegrationReason().IsMatch(text))
                    violations.Add($"{path}: marked [Integration] but touches no database or S3");
            }
        }

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

    private static HashSet<string> ReachesStorage(Dictionary<string, string> texts)
    {
        HashSet<string> reaching = texts.Where(file => SharedStorage().IsMatch(file.Value)).Select(file => file.Key).ToHashSet();
        while (true)
        {
            HashSet<string> types = reaching.SelectMany(path => DeclaredType().Matches(texts[path]).Select(match => match.Groups[1].Value)).ToHashSet();
            List<string> more = texts
                .Where(file => !reaching.Contains(file.Key) && Words().Matches(file.Value).Any(word => types.Contains(word.Value)))
                .Select(file => file.Key)
                .ToList();
            if (more.Count == 0)
                return reaching;
            reaching.UnionWith(more);
        }
    }

    private static string ProjectOf(string path) => path[..path.LastIndexOf('/')];

    private static IEnumerable<string> TestFiles(string suffix) =>
        RepoFiles.Under("tests", suffix)
            .Select(path => Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.StartsWith(SharedHelpers, StringComparison.Ordinal));

    private static IEnumerable<(string Where, string Text)> Lines(string path) =>
        File.ReadLines(Path.Combine(RepoRoot.Path, path)).Select((text, index) => ($"{path}:{index + 1}", text.Trim()));

    [GeneratedRegex(@"\b(Task\.Delay|Thread\.Sleep|DateTime(Offset)?\.(Utc)?Now|TimeProvider\.System|Stopwatch|Random\.Shared)\b|new Random\(\)")]
    private static partial Regex DependsOnTheRun();

    [GeneratedRegex(@"\b(TestDatabase|TestBuckets)\b")]
    private static partial Regex SharedStorage();

    [GeneratedRegex(@"\b(?:class|record|struct)\s+(\w+)")]
    private static partial Regex DeclaredType();

    [GeneratedRegex(@"\w+")]
    private static partial Regex Words();

    [GeneratedRegex(@"^\s*\[(Fact|Theory)\b", RegexOptions.Multiline)]
    private static partial Regex TestMethod();

    [GeneratedRegex(@"\[Integration\(""([^""]*)""\)\]")]
    private static partial Regex IntegrationReason();

    [GeneratedRegex(@"using (DotNet\.)?Testcontainers|Include=""Testcontainers")]
    private static partial Regex UsesTestcontainers();
}
