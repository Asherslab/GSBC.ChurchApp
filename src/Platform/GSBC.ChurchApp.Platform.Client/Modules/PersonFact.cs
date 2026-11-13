using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record PersonFact(string Key, string Label, int Order, RenderFragment<PersonOnPage> Content, Permission? Requires = null);
