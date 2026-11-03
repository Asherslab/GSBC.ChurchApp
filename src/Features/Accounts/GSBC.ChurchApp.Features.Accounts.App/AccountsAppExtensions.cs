using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.App;

public static class AccountsAppExtensions
{
    public static IServiceCollection AddAccountsApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Accounts", typeof(AccountsAppExtensions).Assembly,
            [new NavEntry("Account", "account", "user")]));
        return services.AddAccountsClient().AddScoped<IPasskeyCeremonies, NativePasskeyCeremonies>().AddScoped<ISignInCodes, NativeSignInCodes>();
    }
}
