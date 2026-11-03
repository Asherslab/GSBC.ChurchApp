using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class LiveDataRules
{
    private static readonly string[] NotYetMigrated =
    [
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Pages/RoleDetail.razor",
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Components/RoleEffectView.razor",
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Pages/RolesAdvanced.razor",
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Pages/PersonAccess.razor",
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Pages/RoleEditor.razor",
        "src/Features/Access/GSBC.ChurchApp.Features.Access.Admin/Pages/Roles.razor",
        "src/Features/Media/GSBC.ChurchApp.Features.Media.Client/MediaLibrary.razor",
    ];

    [Fact]
    public void ComponentsThatLoadDataRefreshThemselves()
    {
        List<string> violations = Directory.EnumerateFiles(Path.Combine(RepoRoot.Path, "src"), "*.razor", SearchOption.AllDirectories)
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => !file.Path.Contains("/obj/") && !file.Path.Contains("/bin/") && !NotYetMigrated.Contains(file.Path))
            .Where(file => InjectsData().IsMatch(file.Text) && LoadsOnStart().IsMatch(file.Text) && !Refreshes().IsMatch(file.Text))
            .Select(file => file.Path)
            .ToList();

        Assert.Empty(violations);
    }

    [GeneratedRegex(@"@inject (I?[A-Za-z]+Client|ILocalFeeds|IOutbox|IPeopleDirectory)\b")]
    private static partial Regex InjectsData();

    [GeneratedRegex(@"\bOn(Initialized|ParametersSet)(Async)?\b")]
    private static partial Regex LoadsOnStart();

    [GeneratedRegex(@"<LiveData\b|<RefreshOn\b|\+=")]
    private static partial Regex Refreshes();
}
