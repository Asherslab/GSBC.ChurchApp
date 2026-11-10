using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public static class AccountsClientExtensions
{
    public static IServiceCollection AddAccountsClient(this IServiceCollection services) =>
        services.AddDestinations(AccountsDestinations.All).AddSingleton<AccountsClient>().AddScoped<IPasskeyCeremonies, BrowserPasskeyCeremonies>().AddScoped<ISignInCodes, NoSignInCodes>();
}
