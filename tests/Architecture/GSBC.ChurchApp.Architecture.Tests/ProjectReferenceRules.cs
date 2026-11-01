namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class ProjectReferenceRules
{
    [Fact]
    public void FeaturesOnlyReferenceThemselvesAndPlatform()
    {
        string src = Path.Combine(RepoRoot.Path, "src");
        List<string> violations = [];

        foreach (string project in Projects.All().Where(path => path.StartsWith(src)))
        {
            string? owner = FeatureOf(project);
            bool isPlatform = Path.GetRelativePath(src, project).StartsWith("Platform");
            if (owner is null && !isPlatform)
                continue;

            foreach (string reference in Projects.ReferencesOf(project))
            {
                string? referencedFeature = FeatureOf(reference);
                if (referencedFeature is not null && referencedFeature != owner)
                    violations.Add($"{Projects.NameOf(project)} → {Projects.NameOf(reference)}");
            }
        }

        Assert.Empty(violations);
    }

    private static string? FeatureOf(string project)
    {
        string[] parts = Path.GetRelativePath(Path.Combine(RepoRoot.Path, "src"), project).Split(Path.DirectorySeparatorChar);
        return parts is ["Features", var feature, ..] ? feature : null;
    }
}
