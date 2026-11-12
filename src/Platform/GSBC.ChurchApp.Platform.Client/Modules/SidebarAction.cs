using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record SidebarSheet(bool Open, EventCallback<bool> OpenChanged);

public sealed record SidebarAction(string Title, string Icon, RenderFragment<SidebarSheet> Sheet, Permission? Requires = null, int Order = 0);
