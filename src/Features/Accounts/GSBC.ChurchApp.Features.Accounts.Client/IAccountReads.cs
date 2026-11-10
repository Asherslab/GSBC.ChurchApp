using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;

namespace GSBC.ChurchApp.Features.Accounts.Client;

public interface IAccountReads
{
    IReadOnlyList<string> Feeds { get; }

    Task<MyAccount> MineAsync();
}

public sealed class StoredAccount(ILocalFeeds feeds) : IAccountReads
{
    public IReadOnlyList<string> Feeds { get; } = [AccountsFeeds.Mine];

    public async Task<MyAccount> MineAsync()
    {
        IReadOnlyList<StoredRecord> mine = await feeds.ReadPageAsync(new FeedPageQuery(AccountsFeeds.Mine, Limit: 1));
        return mine.Count == 0
            ? throw new InvalidOperationException("Your account shows here once your phone has it.")
            : AccountsContractsModel.Instance.Deserialize<MyAccount>(new MemoryStream(mine[0].Payload));
    }
}
