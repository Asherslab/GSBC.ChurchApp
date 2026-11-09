using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record AccountSection(string Key, int Order, RenderFragment Content);
