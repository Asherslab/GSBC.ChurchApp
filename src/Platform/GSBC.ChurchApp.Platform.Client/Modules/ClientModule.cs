using System.Reflection;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record ClientModule(string Name, Assembly PageAssembly, IReadOnlyList<NavEntry> Navigation, IReadOnlyList<string>? Feeds = null);

public sealed record NavEntry(string Title, string Href, string Icon);
