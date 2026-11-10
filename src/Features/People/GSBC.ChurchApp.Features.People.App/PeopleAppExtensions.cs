using GSBC.ChurchApp.Features.People.Client;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.App;

public static class PeopleAppExtensions
{
    public static IServiceCollection AddPeopleApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("People", typeof(PeopleAppExtensions).Assembly, [], PrivateFeeds: [PeopleFeeds.Mine, PeopleFeeds.Me]));
        return services.AddPeopleClient();
    }
}
