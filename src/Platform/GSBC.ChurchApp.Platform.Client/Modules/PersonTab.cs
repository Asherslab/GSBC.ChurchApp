using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record PersonTab(string Slug, string Title, int Order, RenderFragment<PersonOnPage> Content, Permission? Requires = null);

public sealed record PersonOnPage(string Id, string Name)
{
    public string FirstName => Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Name;
}
