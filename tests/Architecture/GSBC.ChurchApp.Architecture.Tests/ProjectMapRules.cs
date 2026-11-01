using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class ProjectMapRules
{
    private const string Prefix = "GSBC.ChurchApp.";
    private const string MapDocument = "docs/architecture/repo-layout.md";
    private const string MapSection = "## The projects";

    [Fact]
    public void TheTableListsEveryProjectAndNoOthers()
    {
        HashSet<string> listed = TableRow().Matches(Section()).Select(match => match.Groups[1].Value).ToHashSet();
        AssertSameProjects(Projects.All().Select(Projects.NameOf).ToHashSet(), listed);
    }

    [Fact]
    public void TheDiagramShowsEveryProjectAndItsReferences()
    {
        string diagram = MermaidBlock().Match(Section()).Groups[1].Value;
        Dictionary<string, string> labelById = Node().Matches(diagram)
            .ToDictionary(match => match.Groups[1].Value, match => Prefix + match.Groups[2].Value);

        HashSet<string> drawnEdges = [];
        foreach (Match edge in Edge().Matches(diagram))
        {
            foreach (string target in edge.Groups[2].Value.Split('&', StringSplitOptions.TrimEntries))
                drawnEdges.Add($"{labelById[edge.Groups[1].Value]} → {labelById[target]}");
        }

        HashSet<string> realEdges = Projects.All()
            .SelectMany(project => Projects.ReferencesOf(project).Select(reference => $"{Projects.NameOf(project)} → {Projects.NameOf(reference)}"))
            .ToHashSet();

        AssertSameProjects(Projects.All().Select(Projects.NameOf).ToHashSet(), labelById.Values.ToHashSet());
        Assert.Empty(realEdges.Except(drawnEdges).Select(edge => $"missing arrow: {edge}")
            .Concat(drawnEdges.Except(realEdges).Select(edge => $"arrow with no reference: {edge}")));
    }

    private static void AssertSameProjects(HashSet<string> real, HashSet<string> documented) =>
        Assert.Empty(real.Except(documented).Select(name => $"not in {MapDocument}: {name}")
            .Concat(documented.Except(real).Select(name => $"in {MapDocument} but no such project: {name}")));

    private static string Section()
    {
        string document = File.ReadAllText(Path.Combine(RepoRoot.Path, MapDocument));
        int start = document.IndexOf(MapSection, StringComparison.Ordinal);
        int end = document.IndexOf("\n## ", start + MapSection.Length, StringComparison.Ordinal);
        return document[start..(end < 0 ? document.Length : end)];
    }

    [GeneratedRegex(@"^\| `(GSBC\.ChurchApp\.[A-Za-z.]+)`", RegexOptions.Multiline)]
    private static partial Regex TableRow();

    [GeneratedRegex(@"```mermaid\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex MermaidBlock();

    [GeneratedRegex(@"(\w+)\[""([A-Za-z.]+)""\]")]
    private static partial Regex Node();

    [GeneratedRegex(@"^\s*(\w+) --> (.+)$", RegexOptions.Multiline)]
    private static partial Regex Edge();
}
