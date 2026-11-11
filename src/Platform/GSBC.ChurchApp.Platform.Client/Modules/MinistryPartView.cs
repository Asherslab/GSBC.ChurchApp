using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record MinistryOnPage(string Id, string Name, bool Wide);

public sealed record MinistryPartView(string Part, RenderFragment<MinistryOnPage> Content);
