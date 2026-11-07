using System.Xml.Linq;

namespace GSBC.ChurchApp.Architecture.Tests;

internal static class Projects
{
    public static IEnumerable<string> All() =>
        new[] { "src", "tests" }.SelectMany(folder =>
            RepoFiles.Under(folder, ".csproj"));

    public static string NameOf(string project) => Path.GetFileNameWithoutExtension(project);

    public static IEnumerable<string> ReferencesOf(string project) =>
        XDocument.Load(project).Descendants("ProjectReference")
            .Select(reference => Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(project)!,
                reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))));
}
