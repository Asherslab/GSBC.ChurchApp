using GSBC.ChurchApp.Features.People.Client.Components;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Client;

public static class PeopleClientExtensions
{
    private const int AfterYou = 10;

    public static IServiceCollection AddPeopleClient(this IServiceCollection services) =>
        services.AddSingleton<PeopleClient>().AddSingleton(new AccountSection("people", AfterYou, Family));

    private static readonly RenderFragment Family = builder =>
    {
        builder.OpenComponent<AccountPeople>(0);
        builder.CloseComponent();
    };
}
