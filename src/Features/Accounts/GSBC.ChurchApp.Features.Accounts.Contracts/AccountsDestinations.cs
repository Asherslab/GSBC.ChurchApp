using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

public static class AccountsDestinations
{
    public static readonly DestinationPage Account = new("/account", "Account", "user", DestinationAreas.Pages, "More → Account: sign in, passkeys", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Account);
}
