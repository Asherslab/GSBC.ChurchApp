using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.App;

public static class OutreachAppExtensions
{
    public static IServiceCollection AddOutreachApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Outreach", typeof(OutreachAppExtensions).Assembly, [], OutreachFeeds.Public, OutreachFeeds.Private));
        return services.AddOutreachClient();
    }
}
