using GSBC.ChurchApp.Features.Accounts.Admin.Components;
using GSBC.ChurchApp.Features.Accounts.Client;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.People;
using Microsoft.AspNetCore.Components;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Admin;

public static class AccountsAdminExtensions
{
    private static RenderFragment Menu(SidebarSheet sheet) => builder =>
    {
        builder.OpenComponent<AccountMenu>(0);
        builder.AddComponentParameter(1, nameof(AccountMenu.Open), sheet.Open);
        builder.AddComponentParameter(2, nameof(AccountMenu.OpenChanged), sheet.OpenChanged);
        builder.CloseComponent();
    };

    public static IServiceCollection AddAccountsAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Accounts", typeof(AccountsAdminExtensions).Assembly,
            [new NavEntry("People", "people", AppIcons.People, AccountsPermissions.View, Area: NavAreas.People, Summary: "Invite staff, send a recovery link, turn an account off"), new NavEntry("App review", "app-review", AppIcons.AppReview, AccountsPermissions.ReviewAccess, Area: NavAreas.Technical, Summary: "Sign-in codes for Apple's app reviewers"), new NavEntry("Account", "account", AppIcons.Account, Area: NavAreas.You)], PrivateFeeds: [AccountsFeeds.Pins]));
        return services.AddAccountsClient().AddSingleton<AccountsOfficeClient>().AddSingleton<OnlineAccount>().AddSingleton<IAccountReads>(provider => provider.GetRequiredService<OnlineAccount>()).AddSingleton<IPeopleDirectory, AccountsPeopleDirectory>().AddSingleton<IOfficeWeekSource, InvitationsOfficeWeek>().AddSingleton<IPinnedPages, AccountPinnedPages>()
            .AddSingleton(new SidebarAction("Account", AppIcons.Account, null, Menu, Order: 1));
    }
}
