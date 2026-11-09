using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public sealed class AccountPinnedPages(AccountsClient accounts) : IPinnedPages
{
    public IReadOnlyCollection<string> Feeds { get; } = [AccountsFeeds.Pins];

    public async Task<IReadOnlyList<string>> LoadAsync() => (await accounts.GetPinnedPagesAsync()).Hrefs;

    public async Task SaveAsync(IReadOnlyList<string> hrefs) => await accounts.SetPinnedPagesAsync(hrefs);
}
