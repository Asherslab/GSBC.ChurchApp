using GSBC.ChurchApp.Features.Access.Client;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Access.Admin;

public static class AccessAdminExtensions
{
    public static IServiceCollection AddAccessAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Access", typeof(AccessAdminExtensions).Assembly,
        [
            new NavEntry("Roles", "access/roles", "shield", AccessPermissions.View, Area: NavAreas.People, Summary: "What each role lets people see and change"),
            new NavEntry("Who can do what", "access/people", "key", AccessPermissions.View, Area: NavAreas.People, Summary: "Find someone and see or change what they can do"),
        ]));
        return services.AddAccessClient();
    }
}
