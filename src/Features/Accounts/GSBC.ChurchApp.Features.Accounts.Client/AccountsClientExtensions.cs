using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public static class AccountsClientExtensions
{
    public static IServiceCollection AddAccountsClient(this IServiceCollection services) =>
        services.AddSingleton<AccountsClient>().AddScoped<PasskeyCeremonies>().AddSingleton<IPeopleDirectory, AccountsPeopleDirectory>();
}
