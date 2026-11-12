using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class IconRules
{
    private const string Registry = "src/Platform/GSBC.ChurchApp.Platform.Contracts/AppIcons.cs";
    private const string Drawn = "src/Platform/GSBC.ChurchApp.Platform.Ui/IconPaths.cs";

    [Fact]
    public void EachMeaningHasItsOwnIcon()
    {
        List<string> shared = Meanings()
            .GroupBy(meaning => meaning.Icon)
            .Where(icon => icon.Count() > 1)
            .Select(icon => $"\"{icon.Key}\" means {string.Join(" and ", icon.Select(meaning => meaning.Name))}; give each its own icon")
            .ToList();

        Assert.True(shared.Count == 0, string.Join(Environment.NewLine, shared));
    }

    [Fact]
    public void EveryMeaningIsDrawn()
    {
        string drawn = File.ReadAllText(Path.Combine(RepoRoot.Path, Drawn));

        List<string> missing = Meanings()
            .Where(meaning => !drawn.Contains($"[\"{meaning.Icon}\"]", StringComparison.Ordinal))
            .Select(meaning => $"AppIcons.{meaning.Name} is \"{meaning.Icon}\", which IconPaths doesn't draw")
            .ToList();

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    [Fact]
    public void PagesTakeTheirIconFromTheRegistry()
    {
        List<string> raw = RepoFiles.Under("src", ".cs")
            .SelectMany(path => RawIconRegistration().Matches(File.ReadAllText(path))
                .Select(match => $"{Path.GetRelativePath(RepoRoot.Path, path)}: {match.Value.Trim()}… names its icon directly; use AppIcons"))
            .ToList();

        Assert.True(raw.Count == 0, string.Join(Environment.NewLine, raw));
    }

    private static IEnumerable<(string Name, string Icon)> Meanings() =>
        Meaning().Matches(File.ReadAllText(Path.Combine(RepoRoot.Path, Registry)))
            .Select(match => (match.Groups[1].Value, match.Groups[2].Value));

    [GeneratedRegex(@"public const string (\w+) = ""([^""]+)"";")]
    private static partial Regex Meaning();

    [GeneratedRegex(@"new NavEntry\(""[^""]*"",\s*[^,]+,\s*""|new SidebarAction\(""[^""]*"",\s*""|DestinationPage \w+ = new\(""[^""]*"",\s*""[^""]*"",\s*""")]
    private static partial Regex RawIconRegistration();
}
