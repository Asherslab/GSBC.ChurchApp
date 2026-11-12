using GSBC.ChurchApp.Features.Rosters.Client;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Admin;

public static class RostersAdminExtensions
{
    private const string AwayTab = "away";
    private const int AwayOrder = 3;

    public static IServiceCollection AddRostersAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Rosters", typeof(RostersAdminExtensions).Assembly, [], [RostersFeeds.Changes], []));
        services.AddSingleton(new PersonTab(AwayTab, "Days away", AwayOrder, Away, RostersPermissions.Away));
        return services
            .AddRostersClient()
            .AddSingleton<OnlineRosters>()
            .AddSingleton<IRosterReads>(provider => provider.GetRequiredService<OnlineRosters>());
    }

    private static readonly RenderFragment<PersonOnPage> Away = person => builder =>
    {
        builder.OpenComponent<PersonAwayTab>(0);
        builder.AddComponentParameter(1, nameof(PersonAwayTab.Person), person);
        builder.CloseComponent();
    };
}
