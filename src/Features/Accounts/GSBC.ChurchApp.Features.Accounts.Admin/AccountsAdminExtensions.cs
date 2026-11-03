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
            [new NavEntry("People", "people", "users", AccountsPermissions.View, Group: NavGroups.People, Summary: "Invite staff, send a recovery link, turn an account off"), new NavEntry("Account", "account", "user", Group: NavGroups.You)]));
        return services.AddAccountsClient();
    }
}
