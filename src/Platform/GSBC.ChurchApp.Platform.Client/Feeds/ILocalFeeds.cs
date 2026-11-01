namespace GSBC.ChurchApp.Platform.Client.Feeds;

public interface ILocalFeeds
{
    bool IsAvailable { get; }

    SyncState State { get; }

    event Action<IReadOnlySet<string>>? FeedsChanged;

    event Action? StateChanged;

    ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query);

    ValueTask<StoreInfo> InfoAsync();

    ValueTask RefreshAsync();
}
