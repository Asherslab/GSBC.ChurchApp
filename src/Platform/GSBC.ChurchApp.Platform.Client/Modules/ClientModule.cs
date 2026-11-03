using System.Reflection;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record ClientModule(string Name, Assembly PageAssembly, IReadOnlyList<NavEntry> Navigation, IReadOnlyList<string>? Feeds = null, IReadOnlyList<string>? PrivateFeeds = null);

public sealed record NavEntry(string Title, string Href, string Icon, Permission? Requires = null, NavPlacement Placement = NavPlacement.More, string? Group = null, string? Summary = null);

public enum NavPlacement
{
    Tab,
    More,
}
