using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class SaveRules
{
    private static readonly Dictionary<string, string> NotEditingForms = new()
    {
        ["src/Features/Configuration/GSBC.ChurchApp.Features.Configuration.Client/Components/Drafts/ForEveryoneSheet.razor"] = "a one-step choice of how a draft goes out",
        ["src/Features/Accounts/GSBC.ChurchApp.Features.Accounts.Admin/Pages/People.razor"] = "a list with a search and filters",
        ["src/Features/Events/GSBC.ChurchApp.Features.Events.Client/TellPeopleSheet.razor"] = "send or skip, after the event is already saved",
        ["src/Platform/GSBC.ChurchApp.Platform.Ui/DesignGallery.razor"] = "the component gallery, which keeps nothing",
    };

    [Fact]
    public void FormsThatSaveAskBeforeTheirChangesAreLost()
    {
        List<string> unguarded = Razor()
            .Where(file => !NotEditingForms.ContainsKey(file.Path))
            .Where(file => Edits().IsMatch(file.Text) && Saves().IsMatch(file.Text) && !file.Text.Contains("<UnsavedGuard", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(unguarded);
    }

    [Fact]
    public void TheReviewedExceptionsStillExist() =>
        Assert.DoesNotContain(NotEditingForms.Keys, path => !File.Exists(Path.Combine(RepoRoot.Path, path)));

    private static IEnumerable<(string Path, string Text)> Razor() =>
        RepoFiles.Under("src", ".razor")
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)));

    [GeneratedRegex(@"<(TextField|SelectField|RichTextEditor|SearchPicker|ChipPicker|ColourField|PictureField)\b|@bind-Value")]
    private static partial Regex Edits();

    [GeneratedRegex(@"Busy=""|<SaveBar\b")]
    private static partial Regex Saves();
}
