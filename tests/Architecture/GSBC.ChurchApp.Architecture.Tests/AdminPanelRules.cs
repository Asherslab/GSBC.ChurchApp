extern alias ApiHost;
extern alias AdminHost;

using System.Reflection;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class AdminPanelRules
{
    [Fact]
    public void ThePermissionsTheServerCountsAsAdminAreasAreExactlyTheOnesTheAdminSidebarShows()
    {
        using RoutesOnly<ApiHost::Program> api = new();
        HashSet<string> opening = api.Services.GetRequiredService<PermissionCatalog>().OpeningAdminAreas.Select(permission => permission.Node).ToHashSet();
        HashSet<string> sidebar = AdminSidebar().Select(entry => entry.Requires?.Node).OfType<string>().ToHashSet();

        Assert.NotEmpty(sidebar);
        Assert.Empty(sidebar.Except(opening).Select(node => $"{node} opens a page in the admin sidebar but isn't marked opensAdminArea")
            .Concat(opening.Except(sidebar).Select(node => $"{node} is marked opensAdminArea but no page in the admin sidebar needs it")));
    }

    private static IEnumerable<NavEntry> AdminSidebar()
    {
        Assembly host = typeof(AdminHost::GSBC.ChurchApp.Hosts.Admin.App).Assembly;
        ClientModules modules = new(host);
        ServiceCollection services = new();
        foreach (MethodInfo addAdmin in host.GetReferencedAssemblies().Select(Assembly.Load)
                     .SelectMany(assembly => assembly.GetExportedTypes())
                     .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                     .Where(method => method.Name.EndsWith("Admin") && method.GetParameters().Select(parameter => parameter.ParameterType)
                         .SequenceEqual([typeof(IServiceCollection), typeof(ClientModules)])))
            addAdmin.Invoke(null, [services, modules]);
        return modules.Navigation;
    }
}
