using System.Xml.Linq;

namespace GSBC.ChurchApp.Architecture.Tests;

internal static class Projects
{
    public static IEnumerable<string> All() =>
        new[] { "src", "tests" }.SelectMany(folder =>
            Directory.EnumerateFiles(Path.Combine(RepoRoot.Path, folder), "*.csproj", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}")));

    public static string NameOf(string project) => Path.GetFileNameWithoutExtension(project);

    public static IEnumerable<string> ReferencesOf(string project) =>
        XDocument.Load(project).Descendants("ProjectReference")
            .Select(reference => Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(project)!,
                reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))));
}
