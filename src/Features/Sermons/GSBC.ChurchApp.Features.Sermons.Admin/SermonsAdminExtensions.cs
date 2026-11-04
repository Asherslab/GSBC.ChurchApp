using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Admin;

public static class SermonsAdminExtensions
{
    public static IServiceCollection AddSermonsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Sermons", typeof(SermonsAdminExtensions).Assembly, []));
        return services.AddSermonsClient();
    }
}
