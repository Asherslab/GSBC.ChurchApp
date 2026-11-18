namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class SectionRules
{
    private const string TheOneWrapper = "src/Platform/GSBC.ChurchApp.Platform.Ui/StableSection.razor";

    [Fact]
    public void SectionsAreFilledThroughStableSectionSoTypingKeepsFocus()
    {
        List<string> violations = RepoFiles.Under("src", ".razor")
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => file.Path != TheOneWrapper && file.Text.Contains("<SectionContent", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(violations);
    }
}
