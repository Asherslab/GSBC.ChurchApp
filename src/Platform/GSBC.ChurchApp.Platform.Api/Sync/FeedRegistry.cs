namespace GSBC.ChurchApp.Platform.Api.Sync;

public sealed class FeedRegistry(IEnumerable<FeedDefinition> feeds)
{
    public IReadOnlyDictionary<string, FeedDefinition> ByName { get; } = feeds.ToDictionary(feed => feed.Name);
}
