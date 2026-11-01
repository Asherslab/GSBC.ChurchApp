namespace GSBC.ChurchApp.Platform.Client.Feeds;

public enum SyncState
{
    Stopped,
    Connecting,
    Live,
    Offline,
}

public enum FeedOrder
{
    SortKey,
    StartsAt,
}

public sealed record FeedPageQuery(string Feed, FeedOrder Order = FeedOrder.SortKey, bool Descending = false, int Limit = 50, int Offset = 0, string? GroupKey = null);

public sealed record StoredRecord(string Id, long Version, byte[] Payload, string? SortKey, DateTime? StartsAt, DateTime? EndsAt, string? GroupKey);

public sealed record StoredFeed(string Feed, int Rows, long Bytes, long Cursor);

public sealed record StoreInfo(long FileBytes, IReadOnlyList<StoredFeed> Feeds);
