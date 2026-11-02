using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Admin;

public static class AccountsAdminExtensions
{
    public static IServiceCollection AddAccountsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Accounts", typeof(AccountsAdminExtensions).Assembly,
            [new NavEntry("People", "people", "users", AccountsPermissions.View), new NavEntry("Account", "account", "user"),
                new NavEntry("Preview on your phone", "link-phone", "smartphone")]));
        return services.AddAccountsClient();
    }
}
