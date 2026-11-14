using System.Xml.Linq;

namespace GSBC.ChurchApp.Architecture.Tests;

internal static class Projects
{
    private const string AppHost = "src/Hosts/App/GSBC.ChurchApp.Hosts.App/GSBC.ChurchApp.Hosts.App.csproj";

    public static IEnumerable<string> All() =>
        new[] { "src", "tests" }.SelectMany(folder =>
            RepoFiles.Under(folder, ".csproj"));

    public static string NameOf(string project) => Path.GetFileNameWithoutExtension(project);

    public static IEnumerable<string> ReferencesOf(string project) =>
        XDocument.Load(project).Descendants("ProjectReference")
            .Select(reference => Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(project)!,
                reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))));

    public static IEnumerable<string> InTheApp()
    {
        HashSet<string> seen = [];
        Stack<string> waiting = new([Path.Combine(RepoRoot.Path, AppHost)]);
        while (waiting.TryPop(out string? project))
        {
            if (!seen.Add(project))
                continue;
            foreach (string reference in ReferencesOf(project))
                waiting.Push(reference);
        }
        return seen;
    }
}
