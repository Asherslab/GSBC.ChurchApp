using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Admin;

public static class AccountsAdminExtensions
{
    public static IServiceCollection AddAccountsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Accounts", typeof(AccountsAdminExtensions).Assembly,
            [new NavEntry("People", "people", "users", AccountsPermissions.View, Area: NavAreas.People, Summary: "Invite staff, send a recovery link, turn an account off"), new NavEntry("App review", "app-review", "smartphone", AccountsPermissions.ReviewAccess, Area: NavAreas.Technical, Summary: "Sign-in codes for Apple's app reviewers"), new NavEntry("Account", "account", "user", Area: NavAreas.You)], PrivateFeeds: [AccountsFeeds.Pins]));
        return services.AddAccountsClient().AddSingleton<AccountsOfficeClient>().AddSingleton<OnlineAccount>().AddSingleton<IAccountReads>(provider => provider.GetRequiredService<OnlineAccount>()).AddSingleton<IPeopleDirectory, AccountsPeopleDirectory>().AddSingleton<IOfficeWeekSource, InvitationsOfficeWeek>().AddSingleton<IPinnedPages, AccountPinnedPages>();
    }
}
