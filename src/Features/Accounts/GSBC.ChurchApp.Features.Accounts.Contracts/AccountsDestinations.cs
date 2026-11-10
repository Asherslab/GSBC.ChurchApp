using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

public static class AccountsDestinations
{
    public static readonly DestinationPage Account = new("/account", "Account", "user", DestinationAreas.Pages, "More → Account: sign in, your name and family", Audience.Everyone);

    public static readonly DestinationPage Security = new("/account/security", "Security", "key", DestinationAreas.Pages, "More → Account → Security: passkeys and where you're signed in", Audience.Everyone);

    public static readonly DestinationSet All = DestinationSet.Of(Account, Security);
}
