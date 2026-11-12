using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record MinistryOnPage(string Id, string Name, bool Wide, bool MayManage = false, bool HasAttendees = false, bool MayStartChats = false, bool TeamMayStartChats = false);

public sealed record MinistryPartView(string Part, RenderFragment<MinistryOnPage> Content);
