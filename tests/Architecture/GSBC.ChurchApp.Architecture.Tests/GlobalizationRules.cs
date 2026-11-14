using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class GlobalizationRules
{
    [Fact]
    public void TheAppNeverLooksUpACulture()
    {
        List<string> lookups = Projects.InTheApp()
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

    [GeneratedRegex(@"CultureInfo\.GetCultureInfo\(|new CultureInfo\(|CultureInfo\.CreateSpecificCulture\(")]
    private static partial Regex CultureLookup();
}
