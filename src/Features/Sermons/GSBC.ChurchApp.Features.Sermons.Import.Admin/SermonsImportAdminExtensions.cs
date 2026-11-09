using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Import.Admin;

public static class SermonsImportAdminExtensions
{
    public static IServiceCollection AddSermonsImportAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Sermons import", typeof(SermonsImportAdminExtensions).Assembly,
            [new NavEntry("Import from the website", "sermons-import", "globe", SermonsImportPermissions.Import, Area: NavAreas.SundaysAndSermons,
                Summary: "Bring every sermon across from the old website, once")]));
        return services.AddSingleton<SermonsImportClient>();
    }
}
