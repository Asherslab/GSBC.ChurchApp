namespace GSBC.ChurchApp.Platform.Client.Feeds;

public interface IFeedSignals
{
    SyncState State { get; }

    event Action<IReadOnlySet<string>>? FeedsChanged;

    event Action? StateChanged;

    void Follow(IEnumerable<string> feeds);
}
