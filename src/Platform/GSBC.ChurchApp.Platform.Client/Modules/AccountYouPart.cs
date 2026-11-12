using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record AccountYouPart(string Key, int Order, RenderFragment Content);
