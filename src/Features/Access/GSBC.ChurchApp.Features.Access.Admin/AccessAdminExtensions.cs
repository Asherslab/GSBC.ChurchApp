using GSBC.ChurchApp.Features.Access.Admin.Components;
using GSBC.ChurchApp.Features.Access.Client;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Admin;

public static class AccessAdminExtensions
{
    private const string RolesTab = "roles";
    private const string AbilitiesTab = "can-do";

    public static IServiceCollection AddAccessAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Access", typeof(AccessAdminExtensions).Assembly, []));
        services.AddSingleton(new PersonTab(RolesTab, "Roles", 0, Roles, AccessPermissions.View));
        services.AddSingleton(new PersonTab(AbilitiesTab, "What they can do", 1, CanDo, AccessPermissions.View));
        return services.AddAccessClient();
    }

    private static readonly RenderFragment<PersonOnPage> Roles = person => builder =>
    {
        builder.OpenComponent<PersonRoles>(0);
        builder.AddComponentParameter(1, nameof(PersonRoles.Person), person);
        builder.CloseComponent();
    };

    private static readonly RenderFragment<PersonOnPage> CanDo = person => builder =>
    {
        builder.OpenComponent<PersonCanDo>(0);
        builder.AddComponentParameter(1, nameof(PersonCanDo.Person), person);
        builder.CloseComponent();
    };
}
