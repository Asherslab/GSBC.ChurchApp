using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class GalleryRules
{
    private const string SharedComponents = "src/Platform/GSBC.ChurchApp.Platform.Ui/";

    private static readonly string[] GallerySources =
    [
        "src/Platform/GSBC.ChurchApp.Platform.Ui/DesignGallery.razor",
        "src/Hosts/App/GSBC.ChurchApp.Hosts.App/Pages/Gallery.razor",
        "src/Hosts/GSBC.ChurchApp.Hosts.Admin/Pages/Gallery.razor",
    ];

    private static readonly Dictionary<string, string> NotShown = new()
    {
        ["_Imports"] = "not a component",
        ["DesignGallery"] = "the gallery itself",
        ["AppTheme"] = "applies the church's theme to the whole page",
        ["Can"] = "shows or hides by permission; nothing of its own to see",
        ["EditScope"] = "tells an unsaved-changes guard that something changed",
        ["LiveData"] = "loads data from a feed; the gallery has no feeds",
        ["RefreshOn"] = "reloads when feeds change; nothing to see",
        ["AddressWatch"] = "tells a page its address moved; nothing to see",
        ["UnsavedGuard"] = "asks before leaving with unsaved changes; nothing to see",
        ["News/PostTextBlock"] = "one text block of PostEditor, shown in it",
        ["ModuleRouter"] = "routes pages",
        ["ModuleNav"] = "the admin sidebar, on every admin page",
        ["JumpTo"] = "the admin panel's ⌘K search, on every admin page",
        ["InboxBell"] = "reads the person's own notifications",
        ["WaitingToSend"] = "reads the phone's outbox",
        ["SyncIndicator"] = "reads the phone's sync state",
        ["StorageSections"] = "reads what the phone has stored",
        ["SplitView"] = "renders through Workbench; kept until wave 2 moves its callers",
        ["PeopleRow"] = "reads the people of a real place",
        ["PeopleSheet"] = "reads the people of a real place",
        ["PersonFinder"] = "searches the church's people",
        ["AudioArt"] = "needs a real sermon's audio",
        ["AudioControls"] = "needs a real sermon's audio",
        ["AudioPreview"] = "needs a real sermon's audio",
        ["MediaCover"] = "needs a stored picture",
        ["MediaImage"] = "needs a stored picture",
        ["MediaPreview"] = "needs a stored picture",
        ["MediaShownAt"] = "needs a stored picture",
        ["MediaVideo"] = "needs a stored video",
        ["Content/"] = "pickers that read the church's media, fonts and audiences",
        ["Destinations/"] = "read the app's places from the server",
        ["Surfaces/"] = "draw the configured Home and tiles; shown in App styling's preview",
        ["Tabs/"] = "the phone's tab bar and More list, on every phone page",
    };

    [Fact]
    public void EverySharedComponentIsInTheGallery()
    {
        string gallery = string.Concat(GallerySources.Select(path => File.ReadAllText(Full(path))));

        List<string> missing = RepoFiles.Under("src/Platform/GSBC.ChurchApp.Platform.Ui", ".razor")
            .Select(Relative)
            .Select(path => path[SharedComponents.Length..^".razor".Length])
            .Where(name => !NotShown.Keys.Any(skipped => skipped.EndsWith('/') ? name.StartsWith(skipped, StringComparison.Ordinal) : name == skipped))
            .Where(name => !Regex.IsMatch(gallery, $@"<{Regex.Escape(name[(name.LastIndexOf('/') + 1)..])}[\s/>]"))
            .Select(name => $"{name} isn't in the design gallery (DesignGallery.razor); show it there with a line on when to use it, or say in GalleryRules why it can't be shown.")
            .ToList();

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    private static string Relative(string path) => Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string Full(string relative) => Path.Combine(RepoRoot.Path, relative.Replace('/', Path.DirectorySeparatorChar));
}
