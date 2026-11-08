using System.Reflection;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record ClientModule(string Name, Assembly PageAssembly, IReadOnlyList<NavEntry> Navigation, IReadOnlyList<string>? Feeds = null, IReadOnlyList<string>? PrivateFeeds = null);

public sealed record NavEntry(string Title, string Href, string Icon, Permission? Requires = null, string? Group = null, string? Summary = null, IReadOnlyList<string>? AlsoUnder = null);
