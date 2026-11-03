namespace GSBC.ChurchApp.Platform.Client.Feeds;

public interface ILocalFeeds : IFeedSignals
{
    bool IsAvailable { get; }

    ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query);

    ValueTask<StoreInfo> InfoAsync();

    ValueTask RefreshAsync();
}
