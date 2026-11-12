using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class GlobalizationRules
{
    private const string AppHost = "src/Hosts/App/GSBC.ChurchApp.Hosts.App/GSBC.ChurchApp.Hosts.App.csproj";

    [Fact]
    public void TheAppNeverLooksUpACulture()
    {
        List<string> lookups = AppProjects()
            .Select(project => Path.GetRelativePath(RepoRoot.Path, Path.GetDirectoryName(project)!))
            .SelectMany(folder => RepoFiles.Under(folder, ".cs").Concat(RepoFiles.Under(folder, ".razor")))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => CultureLookup().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepoRoot.Path, path))
            .ToList();

        Assert.True(lookups.Count == 0,
            "The app runs with invariant globalization, so a named culture throws there. Parse and format with CultureInfo.InvariantCulture and explicit formats:"
            + Environment.NewLine + string.Join(Environment.NewLine, lookups));
    }

    private static IEnumerable<string> AppProjects()
    {
        HashSet<string> seen = [];
        Stack<string> waiting = new([Path.Combine(RepoRoot.Path, AppHost)]);
        while (waiting.TryPop(out string? project))
        {
            if (!seen.Add(project))
                continue;
            foreach (string reference in Projects.ReferencesOf(project))
                waiting.Push(reference);
        }
        return seen;
    }

    [GeneratedRegex(@"CultureInfo\.GetCultureInfo\(|new CultureInfo\(|CultureInfo\.CreateSpecificCulture\(")]
    private static partial Regex CultureLookup();
}
